namespace TokenService.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using TokenService.AuthFilter;
    using TokenService.Models;
    using TokenService.Service;

    [ApiController]
    [Route("[controller]")]
    public class TokenServiceController : ControllerBase
    {
        private readonly ILogger<TokenServiceController> _logger;
        private readonly TokenProvider tokenProvider;

        public TokenServiceController(TokenProvider tokenProvider, ILogger<TokenServiceController> logger)
        {
            _logger = logger;
            this.tokenProvider = tokenProvider;
        }

        [TypeFilter(typeof(S2SAuthFilter))]
        [HttpPost("generateTokenPair")]
        public IActionResult GenerateTokenPair([FromBody] GenerateTokenRequest generateTokenRequest)
        {
            (var accessToken, var refreshToken) = tokenProvider.GenerateTokenPair(generateTokenRequest.Claims);
            return Ok(new GenerateTokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("refreshTokenPair")]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            (var newAccessToken, var newRefreshToken) = tokenProvider.RefreshTokenPair(refreshTokenRequest.AccessToken, refreshTokenRequest.RefreshToken);
            return Ok(new GenerateTokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        [HttpPost("validateAccessToken")]
        public IActionResult ValidateAccessToken([FromBody] ValidateTokenRequest validateTokenRequest)
        {
            if (string.IsNullOrEmpty(validateTokenRequest.AccessToken))
                return BadRequest("Access token is required.");
            var isValid = tokenProvider.ValidateAccessToken(validateTokenRequest.AccessToken, out _);
            return Ok(new ValidateTokenResponse() { isValid = isValid });
        }

        [HttpPost("validateRefreshToken")]
        public IActionResult ValidateRefreshToken([FromBody] ValidateTokenRequest validateTokenRequest)
        {
            if (string.IsNullOrEmpty(validateTokenRequest.RefreshToken))
                return BadRequest("Refresh token is required.");
            var isValid = tokenProvider.ValidateRefreshToken(validateTokenRequest.RefreshToken, out _);
            return Ok(new ValidateTokenResponse() { isValid = isValid });
        }

        [HttpPost("validateTokenPair")]
        public IActionResult ValidateTokenPair([FromBody] ValidateTokenRequest validateTokenRequest)
        {
            if (string.IsNullOrEmpty(validateTokenRequest.AccessToken))
                return BadRequest("Access token is required.");
            if (string.IsNullOrEmpty(validateTokenRequest.RefreshToken))
                return BadRequest("Refresh token is required.");
            var isValid = tokenProvider.ValidateTokenPair(validateTokenRequest.AccessToken, validateTokenRequest.RefreshToken, out _);
            return Ok(new ValidateTokenResponse() { isValid = isValid });
        }

        [TypeFilter(typeof(S2SAuthFilter))]
        [HttpPost("disableRefreshToken")]
        public IActionResult DisableRefreshToken([FromBody] DisableTokenRequest disableTokenRequest)
        {
            if (string.IsNullOrEmpty(disableTokenRequest.RefreshToken))
                return BadRequest("Refresh token is required.");
            tokenProvider.DisableRefreshToken(disableTokenRequest.RefreshToken);
            return Ok();
        }

        [HttpGet("ping")]
        public string Ping()
        {
            return "pong";
        }
    }
}
