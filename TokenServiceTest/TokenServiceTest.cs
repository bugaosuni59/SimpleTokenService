using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TokenService.Controllers;
using TokenService.Service;

namespace TokenServiceTest
{
    public class TokenServiceTest
    {
        private readonly IServiceProvider tokenService;
        private readonly TokenProvider tokenProvider;

        public static IEnumerable<object[]> TestData
        {
            get
            {
                yield return new object[] { new Dictionary<string, string> { { "claim1", "value1" }, { "claim2", "value2" } } };
            }
        }

        public TokenServiceTest()
        {
            var tokenServiceSetup = new TokenServiceSetup();
            this.tokenService = tokenServiceSetup.ServiceProvider;
            this.tokenProvider = tokenService.GetRequiredService<TokenProvider>();
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void TokenServiceControllerTest(Dictionary<string, string> claimsDictionary)
        {
            var controller = this.tokenService.GetRequiredService<TokenServiceController>();
            var request = new TokenService.Models.GenerateTokenRequest { Claims = claimsDictionary };
            var response = controller.GenerateTokenPair(request);
            var okResult = response as OkObjectResult;
            var generateTokenResponse = okResult.Value as TokenService.Models.GenerateTokenResponse;
            Assert.NotNull(generateTokenResponse);
            Assert.NotEmpty(generateTokenResponse.AccessToken);
            Assert.NotEmpty(generateTokenResponse.RefreshToken);

            var validateAccessTokenRequest = new TokenService.Models.ValidateTokenRequest { AccessToken = generateTokenResponse.AccessToken };
            var validateAccessTokenResponse = controller.ValidateAccessToken(validateAccessTokenRequest);
            var validateAccessTokenOkResult = validateAccessTokenResponse as OkObjectResult;
            var validateAccessTokenResponseValue = validateAccessTokenOkResult.Value as TokenService.Models.ValidateTokenResponse;
            Assert.NotNull(validateAccessTokenResponseValue);
            Assert.True(validateAccessTokenResponseValue.isValid);

            var validateRefreshTokenRequest = new TokenService.Models.ValidateTokenRequest { RefreshToken = generateTokenResponse.RefreshToken };
            var validateRefreshTokenResponse = controller.ValidateRefreshToken(validateRefreshTokenRequest);
            var validateRefreshTokenOkResult = validateRefreshTokenResponse as OkObjectResult;
            var validateRefreshTokenResponseValue = validateRefreshTokenOkResult.Value as TokenService.Models.ValidateTokenResponse;
            Assert.NotNull(validateRefreshTokenResponseValue);
            Assert.True(validateRefreshTokenResponseValue.isValid);

            var validateTokenPairRequest = new TokenService.Models.ValidateTokenRequest { AccessToken = generateTokenResponse.AccessToken, RefreshToken = generateTokenResponse.RefreshToken };
            var validateTokenPairResponse = controller.ValidateTokenPair(validateTokenPairRequest);
            var validateTokenPairOkResult = validateTokenPairResponse as OkObjectResult;
            var validateTokenPairResponseValue = validateTokenPairOkResult.Value as TokenService.Models.ValidateTokenResponse;
            Assert.NotNull(validateTokenPairResponseValue);
            Assert.True(validateTokenPairResponseValue.isValid);

            var refreshTokenRequest = new TokenService.Models.RefreshTokenRequest { AccessToken = generateTokenResponse.AccessToken, RefreshToken = generateTokenResponse.RefreshToken };
            var refreshTokenResponse = controller.RefreshToken(refreshTokenRequest);
            var refreshTokenOkResult = refreshTokenResponse as OkObjectResult;
            var refreshTokenResponseValue = refreshTokenOkResult.Value as TokenService.Models.GenerateTokenResponse;
            Assert.NotNull(refreshTokenResponseValue);
            Assert.NotEmpty(refreshTokenResponseValue.AccessToken);
            Assert.NotEmpty(refreshTokenResponseValue.RefreshToken);

            var validateRefreshedTokenPairRequest = new TokenService.Models.ValidateTokenRequest { AccessToken = refreshTokenResponseValue.AccessToken, RefreshToken = refreshTokenResponseValue.RefreshToken };
            var validateRefreshedTokenPairResponse = controller.ValidateTokenPair(validateRefreshedTokenPairRequest);
            var validateRefreshedTokenPairOkResult = validateRefreshedTokenPairResponse as OkObjectResult;
            var validateRefreshedTokenPairResponseValue = validateRefreshedTokenPairOkResult.Value as TokenService.Models.ValidateTokenResponse;
            Assert.NotNull(validateRefreshedTokenPairResponseValue);
            Assert.True(validateRefreshedTokenPairResponseValue.isValid);

            var validateOldRefreshTokenRequest = new TokenService.Models.ValidateTokenRequest { RefreshToken = generateTokenResponse.RefreshToken };
            var validateOldRefreshTokenResponse = controller.ValidateRefreshToken(validateOldRefreshTokenRequest);
            var validateOldRefreshTokenOkResult = validateOldRefreshTokenResponse as OkObjectResult;
            var validateOldRefreshTokenResponseValue = validateOldRefreshTokenOkResult.Value as TokenService.Models.ValidateTokenResponse;
            Assert.NotNull(validateOldRefreshTokenResponseValue);
            Assert.False(validateOldRefreshTokenResponseValue.isValid);

            var disableRefreshTokenRequest = new TokenService.Models.DisableTokenRequest { RefreshToken = generateTokenResponse.RefreshToken };
            var disableRefreshTokenResponse = controller.DisableRefreshToken(disableRefreshTokenRequest);
            Assert.IsType<OkResult>(disableRefreshTokenResponse);

            var validateDisabledRefreshTokenRequest = new TokenService.Models.ValidateTokenRequest { RefreshToken = generateTokenResponse.RefreshToken };
            var validateDisabledRefreshTokenResponse = controller.ValidateRefreshToken(validateOldRefreshTokenRequest);
            var validateDisabledRefreshTokenOkResult = validateDisabledRefreshTokenResponse as OkObjectResult;
            var validateDisabledRefreshTokenResponseValue = validateDisabledRefreshTokenOkResult.Value as TokenService.Models.ValidateTokenResponse;
            Assert.NotNull(validateDisabledRefreshTokenResponseValue);
            Assert.False(validateDisabledRefreshTokenResponseValue.isValid);

            var validateDisabledTokenPairRequest = new TokenService.Models.ValidateTokenRequest { AccessToken = generateTokenResponse.AccessToken, RefreshToken = generateTokenResponse.RefreshToken };
            var validateDisabledTokenPairResponse = controller.ValidateTokenPair(validateDisabledTokenPairRequest);
            var validateDisabledTokenPairOkResult = validateDisabledTokenPairResponse as OkObjectResult;
            var validateDisabledTokenPairResponseValue = validateDisabledTokenPairOkResult.Value as TokenService.Models.ValidateTokenResponse;
            Assert.NotNull(validateDisabledTokenPairResponseValue);
            Assert.False(validateDisabledTokenPairResponseValue.isValid);

            var refreshTokenWithDisabledTokenPairRequest = new TokenService.Models.RefreshTokenRequest { AccessToken = generateTokenResponse.AccessToken, RefreshToken = generateTokenResponse.RefreshToken };
            Assert.Throws<Exception>(() => controller.RefreshToken(refreshTokenWithDisabledTokenPairRequest));
        }


        [Theory]
        [MemberData(nameof(TestData))]
        public void GenerateTokenPairTest(Dictionary<string,string> claimsDictionary)
        {
            (var accessToken, var refreshToken) = this.tokenProvider.GenerateTokenPair(claimsDictionary);
            Console.WriteLine($"Access token = {accessToken}");
            Console.WriteLine($"Refresh token = {refreshToken}");
            Assert.NotEmpty(accessToken);
            Assert.NotEmpty(refreshToken);

            this.tokenProvider.DisableRefreshToken(refreshToken);
            Assert.False(this.tokenProvider.ValidateRefreshToken(refreshToken, out _));
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void ValidateTokenTest(Dictionary<string, string> claimsDictionary) 
        {
            (var accessToken, var refreshToken) = this.tokenProvider.GenerateTokenPair(claimsDictionary);
            Console.WriteLine($"Access token = {accessToken}");
            Console.WriteLine($"Refresh token = {refreshToken}");

            Assert.True(this.tokenProvider.ValidateAccessToken(accessToken, out var validatedAccessTokenClaims));
            Assert.True(this.tokenProvider.ValidateRefreshToken(refreshToken, out var validatedRefreshTokenClaims));
            Assert.True(this.tokenProvider.ValidateTokenPair(accessToken, refreshToken, out var validatedClaims));
            Assert.NotNull(validatedAccessTokenClaims);
            Assert.NotNull(validatedRefreshTokenClaims);
            Assert.NotNull(validatedClaims);
            Assert.NotEmpty(validatedAccessTokenClaims);
            Assert.NotEmpty(validatedClaims);
            Assert.NotEmpty(validatedClaims);
            foreach (var claim in claimsDictionary)
            {
                Assert.True(validatedAccessTokenClaims.ContainsKey(claim.Key));
                Assert.True(validatedAccessTokenClaims[claim.Key] == claim.Value);
                Assert.True(validatedClaims.ContainsKey(claim.Key));
                Assert.True(validatedClaims[claim.Key] == claim.Value);
            }
            foreach (var claimInUsed in this.tokenProvider.ClaimNameInUsed)
            {
                Assert.True(validatedAccessTokenClaims.ContainsKey(claimInUsed));
                Assert.True(validatedRefreshTokenClaims.ContainsKey(claimInUsed));
                Assert.True(validatedClaims.ContainsKey(claimInUsed));
            }

            this.tokenProvider.DisableRefreshToken(refreshToken);
            Assert.False(this.tokenProvider.ValidateRefreshToken(refreshToken, out _));
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void RefreshTokenTest(Dictionary<string, string> claimsDictionary)
        {
            (var oldAccessToken, var oldRefreshToken) = this.tokenProvider.GenerateTokenPair(claimsDictionary);
            Console.WriteLine($"Access token = {oldAccessToken}");
            Console.WriteLine($"Refresh token = {oldRefreshToken}");
            (var accessToken, var refreshToken) = this.tokenProvider.RefreshTokenPair(oldAccessToken, oldRefreshToken);

            Assert.True(this.tokenProvider.ValidateAccessToken(accessToken, out var validatedAccessTokenClaims));
            Assert.True(this.tokenProvider.ValidateRefreshToken(refreshToken, out var validatedRefreshTokenClaims));
            Assert.True(this.tokenProvider.ValidateTokenPair(accessToken, refreshToken, out var validatedClaims));
            Assert.NotNull(validatedAccessTokenClaims);
            Assert.NotNull(validatedRefreshTokenClaims);
            Assert.NotNull(validatedClaims);
            Assert.NotEmpty(validatedAccessTokenClaims);
            Assert.NotEmpty(validatedClaims);
            Assert.NotEmpty(validatedClaims);
            foreach (var claim in claimsDictionary)
            {
                Assert.True(validatedAccessTokenClaims.ContainsKey(claim.Key));
                Assert.True(validatedAccessTokenClaims[claim.Key] == claim.Value);
                Assert.True(validatedClaims.ContainsKey(claim.Key));
                Assert.True(validatedClaims[claim.Key] == claim.Value);
            }
            foreach (var claimInUsed in this.tokenProvider.ClaimNameInUsed)
            {
                Assert.True(validatedAccessTokenClaims.ContainsKey(claimInUsed));
                Assert.True(validatedRefreshTokenClaims.ContainsKey(claimInUsed));
                Assert.True(validatedClaims.ContainsKey(claimInUsed));
            }

            this.tokenProvider.DisableRefreshToken(refreshToken);
            Assert.False(this.tokenProvider.ValidateRefreshToken(refreshToken, out _));

            Assert.False(this.tokenProvider.ValidateRefreshToken(oldRefreshToken, out _));

            Assert.Throws<Exception>(() => this.tokenProvider.RefreshTokenPair(oldAccessToken, oldRefreshToken));
        }
    }
}