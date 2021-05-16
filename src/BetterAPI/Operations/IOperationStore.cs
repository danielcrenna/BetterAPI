using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BetterAPI.Operations
{
    public interface IOperationStore
    {
        Task<IEnumerable<Operation>> GetAsync(CancellationToken cancellationToken);
        Task<Operation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}