using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Caching;
using BetterAPI.ChangeLog;
using BetterAPI.Data;
using BetterAPI.Events;
using BetterAPI.Filtering;
using BetterAPI.Paging;
using BetterAPI.Patch;
using BetterAPI.Reflection;
using BetterAPI.Sorting;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace BetterAPI
{
    [FormatFilter]
    public class ResourceController<T> : ResourceController, IResourceController
        where T : class, IResource
    {
        private readonly IStringLocalizer<ResourceController<T>> _localizer;
        private readonly IResourceDataService<T> _service;
        private readonly IPageQueryStore _store;
        private readonly IResourceEventBroadcaster _resourceEvents;
        private readonly ChangeLogBuilder _changeLog;
        private readonly IOptionsSnapshot<ApiOptions> _options;

        public ResourceController(IStringLocalizer<ResourceController<T>> localizer, 
            IResourceDataService<T> service, 
            IPageQueryStore store, 
            IResourceEventBroadcaster resourceEvents, 
            ChangeLogBuilder changeLog,
            IOptionsSnapshot<ApiOptions> options,
            ILogger<ResourceController> logger) : base(localizer, options, logger)
        {
            _localizer = localizer;
            _service = service;
            _store = store;
            _resourceEvents = resourceEvents;
            _changeLog = changeLog;
            _options = options;
        }

        [HttpOptions]
        [DoNotHttpCache]
        [Display(Description = "Returns all HTTP methods allowed, and other introspection data, for this resource")]
        public void OptionsHeaders()
        {
            // See: https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#744-options-and-link-headers
            // OPTIONS allows a client to retrieve information about a resource,
            // at a minimum by returning the Allow header denoting the valid methods for this resource.
            Response.Headers.TryAdd(HeaderNames.Allow, new StringValues(new[]
            {
                HttpMethods.Get, 
                HttpMethods.Delete, 
                HttpMethods.Post,
                HttpMethods.Put, 
                HttpMethods.Patch
            }));

            // See: https://tools.ietf.org/html/rfc5789#section-3.1
            // Accept-Patch should indicate all supported patch formats:
            if (_options.Value.ApiFormats.HasFlagFast(ApiSupportedMediaTypes.ApplicationJson))
            {
                Response.Headers.Append(ApiHeaderNames.AcceptPatch, ApiMediaTypeNames.Application.JsonMergePatch);
                Response.Headers.Append(ApiHeaderNames.AcceptPatch, ApiMediaTypeNames.Application.JsonPatchJson);
            }
            if (_options.Value.ApiFormats.HasFlagFast(ApiSupportedMediaTypes.ApplicationJson))
            {
                Response.Headers.Append(ApiHeaderNames.AcceptPatch, ApiMediaTypeNames.Application.XmlMergePatch);
                Response.Headers.Append(ApiHeaderNames.AcceptPatch, ApiMediaTypeNames.Application.JsonPatchXml);
            }

            // In addition, services SHOULD include a Link header (see RFC 5988) to point to documentation for the resource in question:
            // Link: <{help}>; rel="help"
            Response.Headers.TryAdd(ApiHeaderNames.Link, $"<{Request.Scheme}://{Request.Host}/{Options.Value.OpenApiUiRoutePrefix?.TrimStart('/')}/index.html#{typeof(T).Name}>; rel=\"help\"");
        }

        [Display(Description = "Returns the data structure for this resource type, usually to inform user interfaces or migrations.")]
        [HttpGet("format")]
        [ProducesResponseType(typeof(ResourceFormat), StatusCodes.Status200OK)]
        public IActionResult GetResourceFormat(ApiVersion version, CancellationToken cancellationToken)
        {
            var format = _changeLog.BuildResourceFormat<T>(version);

            return Ok(new One<ResourceFormat> { Value = format });
        }

        [HttpGet("format/{resourceDll}")]
        [ProducesResponseType(typeof(ResourceFormat), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResourceBinary(ApiVersion version, string resourceDll, CancellationToken cancellationToken)
        {
            // FIXME: add all kinds of protections here

            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => Path.GetFileName(x.Location).Equals(resourceDll, StringComparison.OrdinalIgnoreCase)))
            {
                if (string.IsNullOrWhiteSpace(assembly.Location))
                    continue;
                var buffer = await System.IO.File.ReadAllBytesAsync(assembly.Location, cancellationToken);
                return File(buffer, "application/octet-stream");
            }

            return NotFound();
        }

        [Display(Description = "Returns all saved resources, with optional sorting, filtering, paging, shaping, and search criteria")]
        [HttpGet]
        public IActionResult Get(ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            if (!BuildResourceQuery(_service, HttpContext, _options, out ResourceQuery query))
            {
                return PayloadTooLargeWithDetails(
                    "Requested page size ({0}) was larger than the server's maximum page size ({1}).",
                    query.PageSize!, _options.Value.Paging.MaxPageSize.MaxPageSize);
            }

            var results = Query(query, cancellationToken);
            return Ok(results);
        }

        private static bool BuildResourceQuery(IResourceDataService service, HttpContext context, IOptions<ApiOptions> options, out ResourceQuery query)
        {
            query = new ResourceQuery();

            if (service.SupportsSorting && context.Items.TryGetValue(Constants.SortContextKey, out var sortMap) &&
                sortMap != null)
            {
                query.Sorting = (List<(AccessorMember, SortDirection)>) sortMap;
                context.Items.Remove(Constants.SortContextKey);
            }

            if (service.SupportsFiltering && context.Items.TryGetValue(Constants.FilterOperationContextKey, out var filterValue) &&
                filterValue is List<(string, FilterOperator, string?)> filters)
            {
                query.Filters = filters;
            }

            if (service.SupportsCount)
            {
                // We don't check whether the request asked for counting, because we need counting to determine next page results either way,
                // so we'll attempt to always count the total records if it's supported.

                query.CountTotalRows = true;
                context.Items.Remove(Constants.CountContextKey);
            }

            if (service.SupportsSkip && context.Items.TryGetValue(Constants.SkipOperationContextKey, out var skipValue) &&
                skipValue is int skip)
            {
                query.PageOffset = skip;
                context.Items.Remove(Constants.SkipOperationContextKey);
            }

            if (service.SupportsTop & context.Items.TryGetValue(Constants.TopOperationContextKey, out var topValue) &&
                topValue is int top)
            {
                //
                // "Note that client-driven paging does not preclude server-driven paging.
                // If the page size requested by the client is larger than the default page size
                // supported by the server, the expected response would be the number of results specified by the client,
                // paginated as specified by the server paging settings."
                //

                query.PageSize = top;
                context.Items.Remove(Constants.TopOperationContextKey);
            }

            if (service.SupportsMaxPageSize &&
                context.Items.TryGetValue(Constants.MaxPageSizeContextKey, out var maxPageSizeValue) &&
                maxPageSizeValue is int maxPageSize)
            {
                query.MaxPageSize = maxPageSize;
                context.Items.Remove(Constants.MaxPageSizeContextKey);
            }

            if (service.SupportsShaping && context.Items.TryGetValue(Constants.ShapingOperationContextKey, out var shapingValue) &&
                shapingValue is List<string> include)
            {
                query.Fields = include;
                context.Items.Remove(Constants.ShapingOperationContextKey);
            }

            // If no $skip is provided, assume the query is for the first page
            query.PageOffset ??= 0;

            //
            // "Clients MAY request server-driven paging with a specific page size by specifying a $maxpagesize preference.
            //  The server SHOULD honor this preference if the specified page size is smaller than the server's default page size."
            //
            // Interpretation:
            // - If the client specifies $top, use $top as the page size.
            // - If the client omits $top but specifies $maxpagesize, use the smaller of $maxpagesize and the server's default page size.
            if (!query.PageSize.HasValue)
            {
                if (query.MaxPageSize.HasValue && query.MaxPageSize.Value < options.Value.Paging.MaxPageSize.DefaultPageSize)
                    query.PageSize = query.MaxPageSize.Value;
                else
                    query.PageSize = options.Value.Paging.MaxPageSize.DefaultPageSize;
            }

            // If we still don't have a page size, assume the query is for the default page size
            if(!query.PageSize.HasValue || query.PageSize.Value == 0)
                query.PageSize = options.Value.Paging.MaxPageSize.DefaultPageSize;

            //
            // "If the server can't honor $top and/or $skip,
            // the server MUST return an error to the client informing about it instead of just ignoring the query options.
            // This will avoid the risk of the client making assumptions about the data returned."
            //
            if (query.PageSize.Value > options.Value.Paging.MaxPageSize.MaxPageSize)
            {
                return false;
            }

            if (service.SupportsSearch && context.Items.TryGetValue(Constants.SearchOperationContextKey, out var searchValue) &&
                searchValue is string searchQuery)
            {
                query.SearchQuery = searchQuery;
                context.Items.Remove(Constants.SearchOperationContextKey);
            }

            return true;
        }

        [Display(Description = "Returns the next page from an opaque continuation token")]
        [HttpGet("nextPage/{continuationToken}")]
        public IActionResult GetNextPage(ApiVersion apiVersion, string continuationToken, CancellationToken cancellationToken)
        {
            // suppress any warnings since we rely on the URL itself, not a user-provided query
            HttpContext.Items.Remove(Constants.SortContextKey);
            HttpContext.Items.Remove(Constants.CountContextKey);
            HttpContext.Items.Remove(Constants.SkipOperationContextKey);
            HttpContext.Items.Remove(Constants.TopOperationContextKey);
            HttpContext.Items.Remove(Constants.MaxPageSizeContextKey);
            HttpContext.Items.Remove(Constants.ShapingOperationContextKey);

            var query = _store.GetQueryFromHash(continuationToken);
            if (query == default)
                return NotFound();

            query.PageOffset += query.PageSize.GetValueOrDefault(_options.Value.Paging.MaxPageSize.DefaultPageSize);

            var results = Query(query, cancellationToken);
            return Ok(results);
        }

        private IEnumerable<T> Query(ResourceQuery query, CancellationToken cancellationToken)
        {
            IEnumerable<T> results = _service.Get(query, cancellationToken);

            //
            // "Developers who want to know the full number of records across all pages, MAY include the query parameter
            // $count=true to tell the server to include the count of items in the response.
            // 
            if (query.TotalRows.HasValue)
            {
                HttpContext.Items.TryAdd(Constants.CountResultContextKey, query.TotalRows.Value);
            }

            HttpContext.Items.TryAdd(Constants.QueryContextKey, query);
            
            return results;
        }

        [HttpGet("{id}.{format?}")]
        [Display(Description = "Returns a resource by its unique ID")]
        public IActionResult GetById(Guid id, CancellationToken cancellationToken)
        {
            List<string>? fields = default;

            if (_service.SupportsShaping && HttpContext.Items.TryGetValue(Constants.ShapingOperationContextKey, out var shapingValue) &&
                shapingValue is List<string> include)
            {
                fields = include;
                HttpContext.Items.Remove(Constants.ShapingOperationContextKey);
            }

            if (!_service.TryGetById(id, out var model, out _, fields, true, cancellationToken))
                return NotFound();

            return Ok(model);
        }

        [HttpPost]
        [Display(Description = "Creates a new resource", Prompt = "A new resource")]
        public IActionResult Create([FromBody] T model, CancellationToken cancellationToken)
        {
            if (!TryValidateModel(model))
                return BadRequest(ModelState);

            var uninitialized = model.Id.Equals(Guid.Empty);
            if (uninitialized)
                if (_options.Value.Resources.RequireExplicitIds)
                    return BadRequestWithDetails("The resource's ID was uninitialized.");
                else
                {
                    var accessor = WriteAccessor.Create(model, AccessorMemberTypes.Properties, AccessorMemberScope.Public);
                    if(!accessor.TrySetValue(model, nameof(IResource.Id), Guid.NewGuid()))
                        return BadRequestWithDetails("The resource's ID was uninitialized, and the resource does not permit setting it.");
                }

            // Save a database call if the server set the ID
            if (!uninitialized && _service.TryGetById(model.Id, out var other, out _, null, false, cancellationToken))
            {
                if (other != null)
                {
                    // FIXME: replace with a ValueHash
                    var equivalent = true;
                    var reads = ReadAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public, out var members);
                    foreach (var member in members)
                    {
                        if (!member.CanRead)
                            continue;

                        if (!reads.TryGetValue(model, member.Name, out var left) ||
                            !reads.TryGetValue(other, member.Name, out var right) || !left.Equals(right))
                        {
                            equivalent = false;
                            break;
                        }
                    }

                    if (equivalent)
                    {
                        // See: https://tools.ietf.org/html/rfc7231#section-4.3.3
                        //
                        //  If the result of processing a POST would be equivalent to a
                        //  representation of an existing resource, an origin server MAY redirect
                        //  the user agent to that resource by sending a 303 (See Other) response
                        //  with the existing resource's identifier in the Location field.  This
                        //  has the benefits of providing the user agent a resource identifier
                        //  and transferring the representation via a method more amenable to
                        //  shared caching, though at the cost of an extra request if the user
                        //  agent does not already have the representation cached.
                        //
                        Response.Headers.TryAdd(HeaderNames.Location, $"{Request.Path}/{model.Id}");
                        return SeeOtherWithDetails("This resource already exists. Did you mean to update it?");
                    }
                }

                Response.Headers.TryAdd(HeaderNames.Location, $"{Request.Path}/{model.Id}");
                return BadRequestWithDetails("This resource already exists. Did you mean to update it?");
            }

            if (!BeforeSave(model, out var error))
            {
                return error ?? InternalServerErrorWithDetails("Internal error on BeforeSave");
            }

            //
            // FIXME: The corner-case where we're creating but also have a pre-condition which should block this create operation.
            //        It is unlikely to occur in real life, but technically we should know what the ETag is before we attempt this,
            //        but the LastModifiedDate would always be 'now' since we're not expecting anything to exist.

            if (!_service.TryAdd(model, out _, cancellationToken))
            {
                Logger.LogError(ErrorEvents.ErrorSavingResource, _localizer.GetString("Adding resource {Model} failed to save to the underlying data store."), model);
                return InternalServerErrorWithDetails("An unexpected error occurred saving this resource. An error was logged. Please try again later.");
            }

            _resourceEvents.Created(model);
            return Created($"{Request.Path}/{model.Id}", model);
        }

        [NonAction]
        public virtual bool BeforeSave(T model, out IActionResult? error)
        {
            error = default;
            return true;
        }

        [HttpPut("{id}")]
        [Display(Description = "Updates an existing resource", Prompt = "An existing resource")]
        public IActionResult Update([FromRoute] Guid id, [FromBody] T model, CancellationToken cancellationToken)
        {
            if (!TryValidateModel(model))
                return BadRequest(ModelState);

            var uninitialized = id.Equals(Guid.Empty);
            if (uninitialized)
            {
                Response.Headers.TryAdd(HeaderNames.Location, $"{Request.Path}");
                return BadRequest("This resource's ID is uninitialized. Did you mean to create it?");
            }

            if (!_service.TryGetById(id, out var previous, out _, null, false, cancellationToken) || previous == default)
            {
                Response.Headers.TryAdd(HeaderNames.Location, $"{Request.Path}");
                return NotFoundWithDetails("This resource doesn't exist. Did you mean to create it?");
            }

            if (!_service.TryUpdate(previous, model, out _, cancellationToken))
            {
                Logger.LogError(ErrorEvents.ErrorSavingResource, _localizer.GetString("Updating resource {Model} failed to save to the underlying data store."), model);
                return InternalServerErrorWithDetails("An unexpected error occurred saving this resource. An error was logged. Please try again later.");
            }

            _resourceEvents.Updated(model);
            return Ok(model);
        }
        
        [HttpPatch("{id}")]
        [Display(Description = "Updates a resource specified by its unique ID, using the provided merge format")]
        public IActionResult Patch([FromRoute] Guid id, [FromBody] object model, [FromHeader(Name = ApiHeaderNames.Accept)] 
            string contentType, CancellationToken cancellationToken)
        {
            if (contentType.Equals(ApiMediaTypeNames.Application.JsonMergePatch, StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals(ApiMediaTypeNames.Application.XmlMergePatch, StringComparison.OrdinalIgnoreCase))
            {
                return MergePatch(id, (JsonMergePatch<T>) model, cancellationToken);
            }

            if (contentType.Equals(ApiMediaTypeNames.Application.JsonPatchJson, StringComparison.OrdinalIgnoreCase) ||
                contentType.Equals(ApiMediaTypeNames.Application.JsonPatchXml, StringComparison.OrdinalIgnoreCase))
            {
                return JsonPatch(id, (JsonPatch) model, cancellationToken);
            }

            return UnsupportedMediaTypeWithDetails(
                "Patching a resource requires a valid PATCH format. " +
                "You can obtain a list of available PATCH formats for this resource by making an OPTIONS " +
                "request to the resource endpoint, and consulting the 'Accept-Patch' response header.");
        }

        [HttpDelete("{id}")]
        [Display(Description = "Deletes a resource by its unique ID")]
        public IActionResult DeleteById(Guid id, CancellationToken cancellationToken)
        {
            if(_service.TryDeleteById(id, out var deleted, out var error, cancellationToken))
            {
                return Ok(deleted);
            }

            if (deleted != default)
            {
                return GoneWithDetails("The resource with ID {0} was already deleted from the server.", id);
            }

            if (error)
            {
                Logger.LogError(ErrorEvents.ErrorSavingResource, _localizer.GetString("Deleting resource with ID {Id} failed to delete from the underlying data store."), id);
                return InternalServerErrorWithDetails("An unexpected error occurred deleting this resource. An error was logged. Please try again later.");
            }

            return NotFoundWithDetails("There is no resource with ID {0} on this server.", id);
        }

        #region Embedded Collections

        [Display(Description = "Returns all saved resources, with optional sorting, filtering, paging, shaping, and search criteria")]
        [HttpGet("{id}/{embeddedCollectionName}.{format?}")]
        public IActionResult GetEmbedded(ApiVersion apiVersion, Guid id, string embeddedCollectionName, CancellationToken cancellationToken)
        {
            var members = AccessorMembers.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public);

            foreach (var member in members)
            {
                if (!member.Type.ImplementsGeneric(typeof(IEnumerable<>)) || !member.Type.IsGenericType)
                    continue; // not a collection

                var arguments = member.Type.GetGenericArguments();
                var embeddedCollectionType = arguments[0];

                if (!typeof(IResource).IsAssignableFrom(embeddedCollectionType))
                    continue; // not a resource collection

                if (!_changeLog.TryGetResourceNameForType(embeddedCollectionType, out var name))
                    name = embeddedCollectionType.Name;

                if (!embeddedCollectionName.Equals(name.Pluralize(), StringComparison.OrdinalIgnoreCase))
                    return NotFound();

                var controllerType = typeof(ResourceController<>).MakeGenericType(embeddedCollectionType);
                if (!(HttpContext.RequestServices.GetService(controllerType) is IResourceController controller))
                    return NotFound();

                if(controller is Controller mvcController)
                    mvcController.ControllerContext = new ControllerContext(ControllerContext);

                // FIXME: implement filters and add parent ID in the filter

                return controller.Get(apiVersion, cancellationToken);
            }

            // this is a generic 404, to avoid leaking mappings
            return NotFound();
        }

        #endregion

        [NonAction]
        private IActionResult MergePatch(Guid id, JsonMergePatch<T> model, CancellationToken cancellationToken)
        {
            if (!_service.TryGetById(id, out var resource, out _, null, false, cancellationToken) || resource == default)
                return NotFound();

            ModelState.Clear();
            model.ApplyTo(resource, ModelState);

            if (!TryValidateModel(model))
                return BadRequest(ModelState);

            // FIXME: Update
            // FIXME: NotModified

            _resourceEvents.Updated(resource);
            return Ok(resource);
        }

        [NonAction]
        public IActionResult JsonPatch([FromRoute] Guid id, [FromBody] JsonPatch model, CancellationToken cancellationToken)
        {
            if (!_service.TryGetById(id, out var resource, out _, null, false, cancellationToken) || resource == default)
                return NotFound();

            ModelState.Clear();
            // model.ApplyTo(resource, ModelState);

            if (!TryValidateModel(model))
                return BadRequest(ModelState);

            // FIXME: Update
            // FIXME: NotModified
            
            _resourceEvents.Updated(resource);
            return Ok(resource);
        }
    }
}
