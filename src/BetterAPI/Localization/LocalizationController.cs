using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace BetterAPI.Localization
{
    [Route("{culture}/api/localization")]
    [Route("api/localization")]
    public sealed class LocalizationController : Controller
    {
        private readonly ILocalizationStore _store;
        private readonly IOptionsSnapshot<RequestLocalizationOptions> _options;

        public LocalizationController(ILocalizationStore store, IOptionsSnapshot<RequestLocalizationOptions> options)
        {
            _store = store;
            _options = options;
        }

        [HttpOptions]
        public IActionResult Options()
        {
            Response.Headers.TryAdd(HeaderNames.Allow, new StringValues(new[] { HttpMethods.Get, HttpMethods.Delete, HttpMethods.Post }));

            var defaultCulture = _options.Value.DefaultRequestCulture.UICulture.Name;
            var additionalCultures = _options.Value.SupportedUICultures.Select(x => x.Name).Where(x => x != defaultCulture);

            return Ok(new
            {
                DefaultCulture = defaultCulture,
                AdditionalCultures = additionalCultures
            });
        }
        
        [HttpGet]
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

        //text/x-gettext-translation
    }
}
