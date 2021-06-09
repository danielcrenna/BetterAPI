using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Caching;
using BetterAPI.Data;
using BetterAPI.Http;
using BetterAPI.Http.Throttling;
using BetterAPI.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BetterAPI.Tokens
{
    [InternalController]
    [Display(Name = "Tokens", Description = "Provides API tokens for protected operations.")]
    [DoNotHttpCache]
    public class TokenController : Controller
    {
        private readonly JwtSecurityTokenHandler _handler;
        private readonly IStringLocalizer<TokenController> _localizer;
        private readonly Func<DateTimeOffset> _timestamps;
        private readonly IResourceDataService<User> _users;
        private readonly IOptionsSnapshot<TokenOptions> _options;
        private readonly IEncryptionKeyStore _encryptionKeyStore;

        public TokenController(IStringLocalizer<TokenController> localizer, 
            Func<DateTimeOffset> timestamps, 
            IResourceDataService<User> users,
            IEncryptionKeyStore encryptionKeyStore, 
            IOptionsSnapshot<TokenOptions> options)
        {
            _localizer = localizer;
            _timestamps = timestamps;
            _users = users;
            _options = options;
            _encryptionKeyStore = encryptionKeyStore;
            _handler = new JwtSecurityTokenHandler();
        }

        [AllowAnonymous, HttpPost("tokens")]
        [AllowAnonymousThrottle]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(TokenResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateToken([FromBody] TokenRequestModel model, CancellationToken cancellationToken)
        {
            if (!TryValidateModel(model))
                return BadRequest(ModelState);
            
            var query = new ResourceQuery();
            var user = _users.Get(query, cancellationToken).SingleOrDefault();
            if (user == default)
                return Unauthorized();
            
            var issuedAt = _timestamps();
            var expiresAt = issuedAt + _options.Value.Lifetime;

            var claims = new[]
            {
                // https://tools.ietf.org/html/rfc7519#section-4.1.2
                new Claim(JwtRegisteredClaimNames.Sub, model.Identity ?? string.Empty),

                // https://tools.ietf.org/html/rfc7519#section-4.1.4
                new Claim(JwtRegisteredClaimNames.Exp, expiresAt.ToUnixTimeSeconds().ToString()),
                
                // https://tools.ietf.org/html/rfc7519#section-4.1.6
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Value?.SigningKey ?? throw new InvalidOperationException(
                _localizer.GetString("Bearer authentication requires a valid signing key"))));

            var signing = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            string token;
            switch (_options.Value.Format)
            {
                case TokenFormat.JsonWebToken:
                    token = IssueJwtToken(claims, signing);
                    break;
                case TokenFormat.JsonWebEncryption:
                    var encrypting = await _encryptionKeyStore.GetCredentialsAsync();
                    token = IssueJweToken(claims, signing, encrypting);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var response = new TokenResponseModel { Token = token };

            return Ok(response);
        }

        private string IssueJwtToken(IEnumerable<Claim> claims, SigningCredentials signing)
        {
            var token = new JwtSecurityToken(_options.Value.Issuer, _options.Value.Audience, claims, signingCredentials: signing);

            return _handler.WriteToken(token);
        }

        private string IssueJweToken(IEnumerable<Claim> claims, SigningCredentials signing, EncryptingCredentials encrypting)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Value.Issuer,
                Audience = _options.Value.Audience,
                Subject = new ClaimsIdentity(claims),
                EncryptingCredentials = encrypting,
                SigningCredentials = signing
            };

            return _handler.CreateEncodedJwt(descriptor);
        }
    }
}
