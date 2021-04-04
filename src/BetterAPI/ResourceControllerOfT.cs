﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using BetterAPI.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace BetterAPI
{
    [FormatFilter]
    public class ResourceController<T> : ResourceController where T : class, IResource
    {
        private readonly IResourceDataService<T> _service;
        private readonly IEventBroadcaster _events;

        public ResourceController(IResourceDataService<T> service, IEventBroadcaster events, IOptionsSnapshot<ApiOptions> options,
            ILogger<ResourceController> logger) : base(options, logger)
        {
            _service = service;
            _events = events;
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

        [Display(Description = "Returns all saved resources, with optional sort, ordering, and filter criteria")]
        [HttpGet]
        public IActionResult Get(CancellationToken cancellationToken)
        {
            return Ok(_service.Get(cancellationToken));
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

            if (model.Id.Equals(Guid.Empty))
                return BadRequestWithDetails("The resource's ID was uninitialized.");

            if (_service.TryGetById(model.Id, out _, cancellationToken))
                return BadRequestWithDetails("This resource already exists. Did you mean to update it?");

            //
            // FIXME: The corner-case where we're creating but also have a pre-condition which should block this create operation.
            //        It is unlikely to occur in real life, but technically we should know what the ETag is before we attempt this,
            //        but the LastModifiedDate would always be 'now' since we're not expecting anything to exist.

            if (!_service.TryAdd(model))
            {
                Logger.LogError(ErrorEvents.ErrorSavingResource, "Adding resource {Model} failed to save to the underlying data store.", model);
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
                Logger.LogError(ErrorEvents.ErrorSavingResource, "Deleting resource with ID {Id} failed to delete from the underlying data store.", id);
                return InternalServerErrorWithDetails("An unexpected error occurred saving this resource. An error was logged. Please try again later.");
            }

            return NotFound();
        }
    }
}