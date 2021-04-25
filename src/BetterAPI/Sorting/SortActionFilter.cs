// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using BetterAPI.Extensions;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Sorting
{
    /// <summary>
    ///     See: https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#96-sorting-collections
    /// </summary>
    public sealed class SortActionFilter : CollectionQueryActionFilter<SortOptions>
    {
        private static readonly MethodInfo BuilderMethod;
        private readonly IStringLocalizer<SortActionFilter> _localizer;
        private readonly IOptionsSnapshot<SortOptions> _options;
        private readonly ILogger<SortActionFilter> _logger;

        static SortActionFilter()
        {
            BuilderMethod = typeof(SortActionFilter).GetMethod(nameof(BuildOrderByExpression),
                                BindingFlags.NonPublic | BindingFlags.Static)
                            ?? throw new InvalidOperationException();
        }

        public SortActionFilter(IStringLocalizer<SortActionFilter> localizer, IOptionsSnapshot<SortOptions> options, ILogger<SortActionFilter> logger) : 
            base(localizer, options, logger)
        {
            _localizer = localizer;
            _options = options;
            _logger = logger;
        }

        public override async Task OnValidRequestAsync(Type underlyingType, StringValues clauses, ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var members = AccessorMembers.Create(underlyingType, AccessorMemberTypes.Fields | AccessorMemberTypes.Properties,
                AccessorMemberScope.Public);

            // FIXME: add attribute for model ID discriminator, or fail due to missing "Id"
            if (!members.TryGetValue("Id", out _))
            {
                _logger.LogWarning(_localizer.GetString("Sorting operation was skipped, because the underlying resource does not have an 'Id' property."));
                await next.Invoke();
                return;
            }

            // FIXME: avoid allocation here?
            var sortMap = new List<(AccessorMember, SortDirection)>(clauses.Count);

            foreach (var value in clauses)
            {
                var tokens = value.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length == 0)
                    continue; // (FIXME: add a validation error?)

                var clause = tokens[0];
                var name = clause[(clause.IndexOf('=', StringComparison.Ordinal) + 1)..];
                var sort = tokens.Length > 1 ? tokens[1].ToUpperInvariant() : "ASC";

                if (members.TryGetValue(name, out var member))
                    switch (sort)
                    {
                        case "DESC":
                            sortMap.Add((member, SortDirection.Descending));
                            break;
                        case "ASC":
                            sortMap.Add((member, SortDirection.Ascending));
                            break;
                        default:
                            // FIXME: add a validation error?
                            sortMap.Add((member, SortDirection.Ascending));
                            break;
                    }
            }

            // Default sort must always run after any user-specified sorts:
            // -------------------------------------------------------------------
            // See: https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#983-additional-considerations
            // "Stable order prerequisite: Both forms of paging depend on the collection of items having a stable order. The server
            // MUST supplement any specified order criteria with additional sorts (typically by key) to ensure that items are always
            // ordered consistently.
            //
            foreach (var clause in _options.Value.DefaultSort)
                if (members.TryGetValue(clause.Field, out var member))
                    sortMap.Add((member, clause.Direction));

            context.HttpContext.Items.Add(Constants.SortOperationContextKey, sortMap);
                
            var executed = await next();

            if (!executed.HttpContext.Items.ContainsKey(Constants.SortOperationContextKey))
                return; // the underlying store handled the sort request

            executed.HttpContext.Items.Remove(Constants.SortOperationContextKey);

            _logger.LogWarning(_localizer.GetString("Sorting operation has fallen back to object-level sorting. " +
                                "This means that sorting was not performed by the underlying data store, and is not " +
                                "likely consistent across an entire collection."));

            if (executed.Result is OkObjectResult result)
            {
                var collection = executed.GetResultBody(result, out var settable);
                var method = BuilderMethod.MakeGenericMethod(underlyingType) ?? throw new NullReferenceException();

                // FIXME: support multiple order by expressions 
                var key = sortMap[0].Item1;
                var direction = sortMap[0].Item2;

                // AccessorMember key, SortDirection direction
                var arguments = new object[] {key, direction};
                var orderBy = method.Invoke(null, arguments) ?? throw new InvalidOperationException();

                var lambda = (LambdaExpression) orderBy;
                var compiled = lambda.Compile(); // FIXME: cache me
                var sorted = compiled.DynamicInvoke(collection);

                if (settable)
                    result.Value = sorted;
            }
        }
        
        private static Expression<Func<IEnumerable<T>, IEnumerable<T>>> BuildOrderByExpression<T>(AccessorMember key,
            SortDirection direction)
        {
            var methodName = direction switch
            {
                SortDirection.Ascending => nameof(Enumerable.OrderBy),
                SortDirection.Descending => nameof(Enumerable.OrderByDescending),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };

            // keySelector: x => x.Id
            var sourceType = typeof(T);
            var parameter = Expression.Parameter(sourceType, "x");
            var memberAccess = Expression.MakeMemberAccess(parameter, key.MemberInfo);
            var keySelector = Expression.Lambda(memberAccess, parameter);

            // source: IEnumerable<TSource>
            var source = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(sourceType));

            var orderByMethod = GetOrderByMethod(methodName) ?? throw new NullReferenceException();
            var call = Expression.Call(orderByMethod.MakeGenericMethod(sourceType, key.Type), source, keySelector);
            var lambda = Expression.Lambda<Func<IEnumerable<T>, IEnumerable<T>>>(call, source);
            return lambda;
        }

        private static MethodInfo? GetOrderByMethod(string methodName)
        {
            //
            // IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            // 1: public interface IEnumerable<out T> : IEnumerable { }
            // 2: public delegate TResult Func<in T, out TResult>(T arg);
            var orderByMethods = typeof(Enumerable).GetMethods().Where(x => x.Name == methodName);
            foreach (var method in orderByMethods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 2)
                    continue;

                // FIXME: there must be a better way to avoid a comparer?

                // OrderBy<TSource, TKey>
                if (!parameters
                    .Select(p => p.ParameterType)
                    .SequenceEqual(new[] {typeof(IEnumerable<>), typeof(Func<,>)}, new NameComparer()))
                    continue;

                return method;
            }

            return default;
        }

        private sealed class NameComparer : IEqualityComparer<Type>
        {
            public bool Equals(Type? x, Type? y)
            {
                return x?.Name == y?.Name;
            }

            public int GetHashCode(Type obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}