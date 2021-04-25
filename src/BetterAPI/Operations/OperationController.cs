using BetterAPI.Enveloping;
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
        public IActionResult Get()
        {
            return Ok(new Envelope<OperationStatus>());
        }
    }
}
