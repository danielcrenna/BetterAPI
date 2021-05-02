using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using BetterAPI.Caching;
using BetterAPI.Data;
using BetterAPI.Paging;
using BetterAPI.Reflection;
using BetterAPI.Sorting;
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
    public class ResourceController<T> : ResourceController where T : class, IResource
    {
        private readonly IStringLocalizer<ResourceController<T>> _localizer;
        private readonly IResourceDataService<T> _service;
        private readonly IPageQueryStore _store;
        private readonly IEventBroadcaster _events;
        private readonly IOptionsSnapshot<ApiOptions> _options;

        public ResourceController(IStringLocalizer<ResourceController<T>> localizer, IResourceDataService<T> service, IPageQueryStore store, IEventBroadcaster events, IOptionsSnapshot<ApiOptions> options,
            ILogger<ResourceController> logger) : base(localizer, options, logger)
        {
            _localizer = localizer;
            _service = service;
            _store = store;
            _events = events;
            _options = options;
        }

        [HttpOptions]
        [DoNotHttpCache]
        [Display(Description = "Returns all HTTP methods allowed by this resource")]
        public void GetOptions()
        {
            // See: https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#744-options-and-link-headers
            // OPTIONS allows a client to retrieve information about a resource,
            // at a minimum by returning the Allow header denoting the valid methods for this resource.
            Response.Headers.TryAdd(HeaderNames.Allow, new StringValues(new[] { HttpMethods.Get, HttpMethods.Delete, HttpMethods.Post }));

            // In addition, services SHOULD include a Link header (see RFC 5988) to point to documentation for the resource in question:
            // Link: <{help}>; rel="help"
            Response.Headers.TryAdd(ApiHeaderNames.Link, $"<{Request.Scheme}://{Request.Host}/{Options.Value.OpenApiUiRoutePrefix?.TrimStart('/')}>; rel=\"help\"");
        }

        [Display(Description = "Returns the next page from an opaque continuation token")]
        [HttpGet("nextPage/{continuationToken}")]
        public IActionResult GetNextPage(ApiVersion apiVersion, string continuationToken, CancellationToken cancellationToken)
        {
            // suppress any warnings since we rely on the URL itself, not a user-provided query
            HttpContext.Items.Remove(Constants.SortContextKey);
            HttpContext.Items.Remove(Constants.CountContextKey);
            HttpContext.Items.Remove(Constants.SkipContextKey);
            HttpContext.Items.Remove(Constants.TopContextKey);
            HttpContext.Items.Remove(Constants.MaxPageSizeContextKey);
            HttpContext.Items.Remove(Constants.ShapingContextKey);

            var query = _store.GetQueryFromHash(continuationToken);
            if (query == default)
                return NotFound();

            query.PageOffset += query.PageSize.GetValueOrDefault(_options.Value.Paging.MaxPageSize.DefaultPageSize);

            var results = Query(query, cancellationToken);
            return Ok(results);
        }

        [Display(Description = "Returns all saved resources, with optional sorting, filtering, and paging criteria")]
        [HttpGet]
        public IActionResult Get(ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var query = new ResourceQuery();
            
            if (_service.SupportsSort && HttpContext.Items.TryGetValue(Constants.SortContextKey, out var sortMap) && sortMap != null)
            {
                query.Sorting = (List<(AccessorMember, SortDirection)>) sortMap;
                HttpContext.Items.Remove(Constants.SortContextKey);
            }

            if (_service.SupportsCount)
            {
                // We don't check whether the request asked for counting, because we need counting to determine next page results either way,
                // so we'll attempt to always count the total records if it's supported.

                query.CountTotalRows = true;
                HttpContext.Items.Remove(Constants.CountContextKey);
            }

            if (_service.SupportsSkip && HttpContext.Items.TryGetValue(Constants.SkipContextKey, out var skipValue) && skipValue is int skip)
            {
                query.PageOffset = skip;
                HttpContext.Items.Remove(Constants.SkipContextKey);
            }

            if (_service.SupportsTop & HttpContext.Items.TryGetValue(Constants.TopContextKey, out var topValue) && topValue is int top)
            {
                //
                // "Note that client-driven paging does not preclude server-driven paging.
                // If the page size requested by the client is larger than the default page size
                // supported by the server, the expected response would be the number of results specified by the client,
                // paginated as specified by the server paging settings."
                //

                query.PageSize = top;
                HttpContext.Items.Remove(Constants.TopContextKey);
            }

            if (_service.SupportsMaxPageSize && HttpContext.Items.TryGetValue(Constants.MaxPageSizeContextKey, out var maxPageSizeValue) && maxPageSizeValue is int maxPageSize)
            {
                query.MaxPageSize = maxPageSize;
                HttpContext.Items.Remove(Constants.MaxPageSizeContextKey);
            }

            if (_service.SupportsShaping && HttpContext.Items.TryGetValue(Constants.ShapingContextKey, out var shapingValue) && shapingValue is List<string> include)
            {
                query.Fields = include;
                HttpContext.Items.Remove(Constants.ShapingContextKey);
            }

            // If no $skip is provided, assume the query is for the first page
            query.PageOffset ??= 0;

            //
            // "Clients MAY request server-driven paging with a specific page size by specifying a $maxpagesize preference.
            //  The server SHOULD honor this preference if the specified page size is smaller than the server's default page size."
            //
            // Interpretation:
            // - If the client specifies $top, use $top as the page size.
            // - If the client omits $top but specified $maxpagesize, use the smaller of $maxpagesize and the server's default page size.
            if (!query.PageSize.HasValue)
            {
                if (query.MaxPageSize.HasValue && query.MaxPageSize.Value < _options.Value.Paging.MaxPageSize.DefaultPageSize)
                    query.PageSize = query.MaxPageSize.Value;
                else
                    query.PageSize = _options.Value.Paging.MaxPageSize.DefaultPageSize;
            }

            //
            // "If the server can't honor $top and/or $skip,
            // the server MUST return an error to the client informing about it instead of just ignoring the query options.
            // This will avoid the risk of the client making assumptions about the data returned."
            //
            if (query.PageSize.Value > _options.Value.Paging.MaxPageSize.MaxPageSize)
            {
                return PayloadTooLargeWithDetails("Requested page size ({0}) was larger than the server's maximum page size ({1}).",
                    query.PageSize, _options.Value.Paging.MaxPageSize.MaxPageSize);
            }

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
            if (!_service.TryGetById(id, out var model, cancellationToken))
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
            if (!uninitialized && _service.TryGetById(model.Id, out _, cancellationToken))
            {
                Response.Headers.TryAdd(HeaderNames.Location, $"{Request.Path}/{model.Id}");
                return BadRequestWithDetails("This resource already exists. Did you mean to update it?");
            }

            //
            // FIXME: The corner-case where we're creating but also have a pre-condition which should block this create operation.
            //        It is unlikely to occur in real life, but technically we should know what the ETag is before we attempt this,
            //        but the LastModifiedDate would always be 'now' since we're not expecting anything to exist.

            if (!_service.TryAdd(model))
            {
                Logger.LogError(ErrorEvents.ErrorSavingResource, _localizer.GetString("Adding resource {Model} failed to save to the underlying data store."), model);
                return InternalServerErrorWithDetails("An unexpected error occurred saving this resource. An error was logged. Please try again later.");
            }

            _events.Created(model);
            return Created($"{Request.Path}/{model.Id}", model);
        }

        [HttpDelete("{id}")]
        [Display(Description = "Deletes a resource by its unique ID")]
        public IActionResult DeleteById(Guid id)
        {
            if(_service.TryDeleteById(id, out var deleted, out var error))
            {
                return Ok(deleted);
            }

            if (deleted != default)
            {
                return StatusCode((int) HttpStatusCode.Gone);
            }

            if (error)
            {
                Logger.LogError(ErrorEvents.ErrorSavingResource, _localizer.GetString("Deleting resource with ID {Id} failed to delete from the underlying data store."), id);
                return InternalServerErrorWithDetails("An unexpected error occurred saving this resource. An error was logged. Please try again later.");
            }

            return NotFound();
        }
    }
}
