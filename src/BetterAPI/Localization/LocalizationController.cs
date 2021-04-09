
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Localization
{
    [Route("api/localization")]
    public sealed class LocalizationController : Controller
    {
        private readonly ILocalizationStore _store;

        public LocalizationController(ILocalizationStore store)
        {
            _store = store;
        }

        [HttpGet("")]
        public IActionResult Get()
        {
            var all = _store.GetAllTranslations(true);
            var models = all.Select(x => new LocalizationViewModel(x.Name, x.Value));
            return Ok(new Envelope<LocalizationViewModel>(models));
        }

        [HttpGet("missing")]
        public IActionResult GetMissing()
        {
            var all = _store.GetAllMissingTranslations(true);
            var models = all.Select(x => new LocalizationViewModel(x.Name, x.Value));
            return Ok(new Envelope<LocalizationViewModel>(models));
        }
    }
}
