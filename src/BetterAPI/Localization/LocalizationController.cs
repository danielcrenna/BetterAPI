using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        [ProducesResponseType(typeof(IEnumerable<LocalizationViewModel>), StatusCodes.Status200OK)]
        public IActionResult Get(CancellationToken cancellationToken)
        {
            var all = _store.GetAllTranslationsByCurrentCulture(true, cancellationToken);
            var models = all.Select(x => new LocalizationViewModel(x.Culture, x.Scope, x.Key, x.Value));
            return Ok(models);
        }

        [HttpGet("missing")]
        [ProducesResponseType(typeof(IEnumerable<LocalizationViewModel>), StatusCodes.Status200OK)]
        public IActionResult GetMissing(CancellationToken cancellationToken)
        {
            var all = _store.GetAllMissingTranslationsByCurrentCulture(true, cancellationToken);
            var models = all.Select(x => new LocalizationViewModel(x.Culture, x.Scope, x.Key, x.Value));
            return Ok(models);
        }

        [HttpGet("missing/{scope}")]
        [ProducesResponseType(typeof(IEnumerable<ScopedLocalizationViewModel>), StatusCodes.Status200OK)]
        public IActionResult GetMissing(string scope, CancellationToken cancellationToken)
        {
            var all = _store.GetAllMissingTranslationsByCurrentCulture(scope, true, cancellationToken);
            var models = all.Select(x => new ScopedLocalizationViewModel(x.Culture, x.Key, x.Value));
            return Ok(models);
        }

        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<LocalizationViewModel>), StatusCodes.Status200OK)]
        public IActionResult GetAll(CancellationToken cancellationToken)
        {
            var all = _store.GetAllTranslations(cancellationToken);
            var models = all.Select(x => new LocalizationViewModel(x.Culture, x.Scope, x.Key, x.Value));
            return Ok(models);
        }

        //text/x-gettext-translation
    }
}
