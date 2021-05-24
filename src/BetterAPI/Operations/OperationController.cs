using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Operations
{
    public class OperationController : Controller
    {
        private readonly IOperationStore _store;

        public OperationController(IOperationStore store)
        {
            _store = store;
        }

        /// <summary>
        /// <see href="https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#1324-operations-resource"/>
        /// </summary>
        /// <returns></returns>
        [HttpGet("operations")]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var operations = await _store.GetAsync(cancellationToken);
            return Ok(operations);
        }

        /// <summary>
        /// <see href="https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#1324-operations-resource"/>
        /// </summary>
        /// <returns></returns>
        [HttpGet("operations/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var operation = await _store.GetByIdAsync(id, cancellationToken);
            if (operation == default)
                return NotFound();

            return Ok(operation);
        }
    }
}
