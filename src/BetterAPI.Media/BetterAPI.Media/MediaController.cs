using System.ComponentModel.DataAnnotations;
using BetterAPI.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace BetterAPI.Media
{
    [InternalController]
    [Display(Name = "Media", Description = "Provides an API for media streaming and file management.")]
    public sealed class MediaController : Controller
    {
        [HttpOptions]
        public void Options()
        {
            Response.Headers.Add(HeaderNames.AcceptRanges, "bytes");
        }
    }
}
