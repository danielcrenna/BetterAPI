using System;
using System.Collections.Generic;
using System.Threading;
using BetterAPI.Reflection;
using BetterAPI.Sorting;
using Microsoft.Extensions.Options;

namespace BetterAPI.Data
{
    public sealed class SqliteResourceDataService<T> : IResourceDataService<T>, IResourceDataServiceSorting<T> 
        where T : class, IResource
    {
        private readonly IOptionsSnapshot<ApiOptions> _options;

        public SqliteResourceDataService(IOptionsSnapshot<ApiOptions> options)
        {
            _options = options;
        }

        public IEnumerable<T> Get(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public bool TryGetById(Guid id, out T? resource, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public bool TryAdd(T model)
        {
            throw new NotImplementedException();
        }

        public bool TryDeleteById(Guid id, out T? deleted, out bool error)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Get(List<(AccessorMember, SortDirection)> sortMap, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}