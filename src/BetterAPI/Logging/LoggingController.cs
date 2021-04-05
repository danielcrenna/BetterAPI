using System.Collections.Generic;
using System.Threading;
using BetterAPI.Filtering;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Logging
{
    [Route("api/logs")]
    public sealed class LoggingController : Controller
    {
        private readonly ILoggingStore _store;

        public LoggingController(ILoggingStore store)
        {
            _store = store;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<LoggingEntry>), StatusCodes.Status200OK)]
        public IActionResult Get(CancellationToken cancellationToken)
        {
            // FIXME: encapsulate query criteria into another filter
            // FIXME: audit use of ToUpperInvariant and remove all possible usages (or get from a cache)
            // FIXME: query is only returning one result when multiple should exist!

            if (HttpContext.Items.TryGetValue(Constants.FilterOperationContextKey, out var filter) && filter != null && filter is List<(AccessorMember, FilterOperator, object?)> filterMap)
            {
                HttpContext.Items.Remove(Constants.FilterOperationContextKey);

                foreach (var (member, @operator, value) in filterMap)
                {
                    if (@operator == FilterOperator.Equal)
                    {
                        return Ok(_store.GetByData(
                            member.Name.ToUpperInvariant(), 
                            value?.ToString()?.ToUpperInvariant(), cancellationToken));
                    }
                }
            }

            return Ok(_store.Get(cancellationToken));
        }
    }
}
