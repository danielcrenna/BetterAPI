// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.ComponentModel.DataAnnotations;
using BetterAPI.Caching;
using BetterAPI.DeltaQueries;
using BetterAPI.Filtering;
using BetterAPI.Guidelines.Cors;
using BetterAPI.Paging;
using BetterAPI.Prefer;
using BetterAPI.RateLimiting;
using BetterAPI.Search;
using BetterAPI.Shaping;
using BetterAPI.Sorting;
using BetterAPI.Tokens;
using BetterAPI.Versioning;

namespace BetterAPI
{
    public sealed class ApiOptions
    {
        public string ApiName { get; set; } = "BetterAPI";
        public string ApiServer { get; set; } = $"BetterAPI-{typeof(ApiOptions).Assembly.GetName().Version}";
        public string ApiDescription { get; set; } = "BetterAPI";
        public string ApiContactName { get; set; } = "Support";

        [EmailAddress]
        public string ApiContactEmail { get; set; } = "support@api.com";

        public ApiSupportedMediaTypes ApiFormats { get; set; } = ApiSupportedMediaTypes.ApplicationJson | 
                                                                 ApiSupportedMediaTypes.ApplicationXml;

        public string? OpenApiUiRoutePrefix { get; set; } = "openapi"; 
        public string? OpenApiSpecRouteTemplate { get; set; } = "/openapi/{documentname}/openapi.json";

        public VersioningOptions Versioning { get; set; } = new VersioningOptions();
        public CacheOptions Cache { get; set; } = new CacheOptions();
        public CorsOptions Cors { get; set; } = new CorsOptions();
        public RateLimitingOptions RateLimiting { get; set; } = new RateLimitingOptions();
        public SortOptions Sort { get; set; } = new SortOptions();
        public FilterOptions Filter { get; set; } = new FilterOptions();
        public PagingOptions Paging { get; set; } = new PagingOptions();
        public IncludeOptions Include { get; set; } = new IncludeOptions();
        public ExcludeOptions Exclude { get; set; } = new ExcludeOptions();
        public PreferOptions Prefer { get; set; } = new PreferOptions();
        public DeltaQueryOptions DeltaQueries { get; set; } = new DeltaQueryOptions();
        public ProblemDetailsOptions ProblemDetails { get; set; } = new ProblemDetailsOptions();
        public TokenOptions Tokens { get; set; } = new TokenOptions();
        public ResourceOptions Resources { get; set; } = new ResourceOptions();
        public SearchOptions Search { get; set; } = new SearchOptions();
    }
}