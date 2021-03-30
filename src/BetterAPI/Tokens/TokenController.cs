using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using BetterAPI.Caching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BetterAPI.Tokens
{
    [DoNotHttpCache]
    public class TokenController : Controller
    {
        private readonly JwtSecurityTokenHandler _handler;
        private readonly IOptionsSnapshot<TokenOptions> _options;

        public TokenController(IOptionsSnapshot<TokenOptions> options)
        {
            _options = options;
            _handler = new JwtSecurityTokenHandler();
        }

        [AllowAnonymous, HttpPost("tokens")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(TokenResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GenerateToken([FromBody] TokenRequestModel model)
        {
            if (!TryValidateModel(model))
                return BadRequest(ModelState);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, model.Identity),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Value.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_options.Value.Issuer, _options.Value.Audience, claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: credentials);

            var response = new TokenResponseModel {Token = _handler.WriteToken(token)};
            return Ok(response);
        }
    }
}
