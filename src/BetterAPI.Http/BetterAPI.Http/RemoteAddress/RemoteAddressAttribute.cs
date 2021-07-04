using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Http.RemoteAddress
{
    public sealed class RemoteAddressAttribute : ServiceFilterAttribute
    {
        public RemoteAddressAttribute() : base(typeof(RemoteAddressFilter))
        {
            IsReusable = true;
        }
    }
}
