// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BetterAPI.Testing
{
    public static class HttpClientExtensions
    {
        public static HttpClient PreferMinimal(this HttpClient client)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(ApiHeaderNames.Prefer, Constants.Prefer.ReturnMinimal);
            return client;
        }

        public static HttpClient PreferRepresentation(this HttpClient client)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(ApiHeaderNames.Prefer,
                Constants.Prefer.ReturnRepresentation);
            return client;
        }

        #region OPTIONS

        public static Task<HttpResponseMessage> OptionsAsync(this HttpClient client, string? requestUri)
        {
            return client.OptionsAsync(CreateUri(requestUri));
        }

        public static Task<HttpResponseMessage> OptionsAsync(this HttpClient client, Uri? requestUri)
        {
            return client.OptionsAsync(requestUri, DefaultCompletionOption);
        }

        public static Task<HttpResponseMessage> OptionsAsync(this HttpClient client, string? requestUri,
            HttpCompletionOption completionOption)
        {
            return client.OptionsAsync(CreateUri(requestUri), completionOption);
        }

        public static Task<HttpResponseMessage> OptionsAsync(this HttpClient client, Uri? requestUri,
            HttpCompletionOption completionOption)
        {
            return client.OptionsAsync(requestUri, completionOption, CancellationToken.None);
        }

        public static Task<HttpResponseMessage> OptionsAsync(this HttpClient client, string? requestUri,
            CancellationToken cancellationToken)
        {
            return client.OptionsAsync(CreateUri(requestUri), cancellationToken);
        }

        public static Task<HttpResponseMessage> OptionsAsync(this HttpClient client, Uri? requestUri,
            CancellationToken cancellationToken)
        {
            return client.OptionsAsync(requestUri, DefaultCompletionOption, cancellationToken);
        }

        public static Task<HttpResponseMessage> OptionsAsync(this HttpClient client, string? requestUri,
            HttpCompletionOption completionOption,
            CancellationToken cancellationToken)
        {
            return client.OptionsAsync(CreateUri(requestUri), completionOption, cancellationToken);
        }

        public static Task<HttpResponseMessage> OptionsAsync(this HttpClient client, Uri? requestUri,
            HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return client.SendAsync(CreateRequestMessage(HttpMethod.Options, requestUri), completionOption,
                cancellationToken);
        }

        #endregion

        #region Request Builder

        private static Uri? CreateUri(string? uri)
        {
            return string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
        }

        private static HttpRequestMessage CreateRequestMessage(HttpMethod method, Uri? uri)
        {
            return new HttpRequestMessage(method, uri)
                {Version = DefaultRequestVersion, VersionPolicy = DefaultVersionPolicy};
        }

        private static readonly Version DefaultRequestVersion = HttpVersion.Version11;
        private const HttpVersionPolicy DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        private const HttpCompletionOption DefaultCompletionOption = HttpCompletionOption.ResponseContentRead;

        #endregion
    }
}