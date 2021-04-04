using System.Collections.Generic;
using System.Threading;
using BetterAPI.Reflection;
using BetterAPI.Sorting;

namespace BetterAPI
{
    public interface IResourceDataServiceSorting<out T> where T : class, IResource
    {
        IEnumerable<T> Get(List<(AccessorMember, SortDirection)> sortMap, CancellationToken cancellationToken);
    }
}