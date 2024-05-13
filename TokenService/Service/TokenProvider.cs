namespace TokenService.Service
{
    using Microsoft.IdentityModel.Tokens;
    using StackExchange.Redis;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using System;
    using TokenService.Config;
    using TokenService.Models;

    public class TokenProvider
    {
        private const string TokenTypeClaimName = "tokenType";
        private const string TokenPairIdClaimName = "tokenPairId";
        private readonly int DefaultAccessTokenExpireMinutes;
        private readonly int DefaultRefreshTokenExpireHours;
        public readonly List<string> ClaimNameInUsed = new List<string>() { TokenPairIdClaimName, TokenTypeClaimName };

        private HashSet<string> validRefreshTokenIds;
        private bool useEnvironmentVariablesFirst;

        private RedisService? redis;
        private readonly string ValidRefreshTokenIdKey = string.Empty;

        private string _signTokenKey = string.Empty;
        private string signTokenKey {
            get { return useEnvironmentVariablesFirst ? Environment.GetEnvironmentVariable("SIGN_TOKEN_KEY") ?? _signTokenKey : _signTokenKey; }
            set => _signTokenKey = value;
        }

        public TokenProvider(TokenServiceConfig tokenServiceConfig) 
        {
            if (tokenServiceConfig == null || string.IsNullOrEmpty(tokenServiceConfig.SIGN_TOKEN_KEY) || string.IsNullOrEmpty(tokenServiceConfig.S2S_KEY))
                throw new Exception("TokenServiceConfig is invalid");
            this.DefaultAccessTokenExpireMinutes = tokenServiceConfig.DefaultAccessTokenExpireMinutes;
            this.DefaultRefreshTokenExpireHours = tokenServiceConfig.DefaultRefreshTokenExpireHours;
            this.signTokenKey = this._signTokenKey = tokenServiceConfig.SIGN_TOKEN_KEY;
            this.useEnvironmentVariablesFirst = tokenServiceConfig.UseEnvironmentVariablesFirst;
            this.validRefreshTokenIds = new HashSet<string>();
            if (tokenServiceConfig.RedisConfig != null)
            {
                if (tokenServiceConfig.RedisConfig.EnableRedis)
                {
                    this.redis = new RedisService(tokenServiceConfig.RedisConfig.ConnectionString);
                    this.ValidRefreshTokenIdKey = tokenServiceConfig.RedisConfig.ValidRefreshTokenIdKeyName;
                    var validRefreshTokenIdsFromRedis = this.redis.db.SetMembers(ValidRefreshTokenIdKey);
                    if (validRefreshTokenIdsFromRedis != null)
                        foreach (var validRefreshTokenId in validRefreshTokenIdsFromRedis)
                            this.validRefreshTokenIds.Add(validRefreshTokenId.ToString());
                }
            }
        }

        public (string, string) GenerateTokenPair(Dictionary<string, string> claimsDictionary)
        {
            string tokenPairId = Guid.NewGuid().ToString();
            var accessToken = GenerateAccessToken(claimsDictionary, tokenPairId);
            var refreshToken = GenerateRefreshToken(tokenPairId);
            this.validRefreshTokenIds.Add(tokenPairId);
            this.redis?.db.SetAdd(this.ValidRefreshTokenIdKey, tokenPairId);
            return (accessToken, refreshToken);
        }

        public (string, string) RefreshTokenPair(string accessToken, string refreshToken)
        {
            if (!ValidateTokenPair(accessToken, refreshToken, out Dictionary<string, string> validatedClaims)) throw new Exception("Invalid token pair");
            foreach (var claimName in ClaimNameInUsed)
                if (!validatedClaims.ContainsKey(claimName)) throw new Exception($"Token claims don't contains claimName={claimName}");
                else validatedClaims.Remove(claimName);
            DisableRefreshToken(refreshToken);
            return GenerateTokenPair(validatedClaims);
        }

        public bool ValidateAccessToken(string jwtToken, out Dictionary<string, string>? validatedClaims)
        {
            var requiredClaims = new Dictionary<string, string> { { TokenTypeClaimName, TokenType.Access.ToString() } };
            return ValidateJwtToken(jwtToken, requiredClaims, out validatedClaims);
        }

        public bool ValidateRefreshToken(string jwtToken, out Dictionary<string, string>? validatedClaims)
        {
            var requiredClaims = new Dictionary<string, string> { { TokenTypeClaimName, TokenType.Refresh.ToString() } };
            if(!ValidateJwtToken(jwtToken, requiredClaims, out validatedClaims))
            {
                validatedClaims = null;
                return false;
            }
            if (validatedClaims == null) 
                return false;
            validatedClaims.TryGetValue(TokenPairIdClaimName, out var tokenPairId);
            if (string.IsNullOrEmpty(tokenPairId)) 
                return false;
            return IsValidRefreshTokenId(tokenPairId);
        }

        public bool ValidateTokenPair(string accessToken, string refreshToken, out Dictionary<string, string> validatedClaims)
        {
            validatedClaims = new Dictionary<string, string>();
            if (!ValidateAccessToken(accessToken, out var accessTokenClaims)) return false;
            if (!ValidateRefreshToken(refreshToken, out var refreshTokenClaims)) return false;
            if (accessTokenClaims == null || refreshTokenClaims == null) return false;
            accessTokenClaims.TryGetValue(TokenPairIdClaimName, out var accessTokenPairId);
            refreshTokenClaims.TryGetValue(TokenPairIdClaimName, out var refreshTokenPairId);
            validatedClaims = new Dictionary<string, string>(accessTokenClaims);
            return accessTokenPairId != null && refreshTokenPairId != null && accessTokenPairId == refreshTokenPairId;
        }

        public bool DisableRefreshToken(string refreshToken)
        {
            var tokenPairId = GetTokenPairId(refreshToken);
            if (!this.validRefreshTokenIds.Contains(tokenPairId))
                return true;
            this.validRefreshTokenIds.Remove(tokenPairId);
            this.redis?.db.SetRemove(this.ValidRefreshTokenIdKey, tokenPairId);
            return true;
        }

        private bool IsValidRefreshTokenId(string tokenPairId)
        {
            return this.validRefreshTokenIds.Contains(tokenPairId);
        }

        private string GetTokenPairId(string jwtToken)
        {
            if (!ValidateJwtToken(jwtToken, new Dictionary<string, string>(), out var claims))
                return string.Empty;
            if (claims == null || !claims.ContainsKey(TokenPairIdClaimName))
                return string.Empty;
            return claims[TokenPairIdClaimName];
        }

        private string GenerateJwtToken(Dictionary<string, string> claimsDictionary, int expireMinutes)
        {
            var signTokenKey = Encoding.ASCII.GetBytes(this.signTokenKey);
            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>();
            foreach (var claim in claimsDictionary)
                claims.Add(new Claim(claim.Key, claim.Value));

            var claimsSubject = new ClaimsIdentity(claims);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsSubject,
                Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(signTokenKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateAccessToken(Dictionary<string, string> claimsDictionary, string tokenPairId)
        {
            var claims = new Dictionary<string, string>(claimsDictionary);
            if (claims.ContainsKey(TokenPairIdClaimName)) throw new Exception($"The claim {TokenPairIdClaimName} is in conflicted");
            else claims.Add(TokenPairIdClaimName, tokenPairId);
            if (claims.ContainsKey(TokenTypeClaimName)) throw new Exception($"The claim {TokenTypeClaimName} is in conflicted");
            else claims.Add(TokenTypeClaimName, TokenType.Access.ToString());
            return GenerateJwtToken(claims, this.DefaultAccessTokenExpireMinutes);
        }

        private string GenerateRefreshToken(string tokenPairId)
        {
            return GenerateJwtToken(new Dictionary<string, string> {
                    { TokenTypeClaimName, TokenType.Refresh.ToString() },
                    { TokenPairIdClaimName, tokenPairId} }, this.DefaultRefreshTokenExpireHours * 60);
        }

        private bool ValidateJwtToken(string jwtToken, Dictionary<string, string> requiredClaims, out Dictionary<string, string>? validatedClaims)
        {
            var signTokenKey = Encoding.ASCII.GetBytes(this.signTokenKey);
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(signTokenKey),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true
            };

            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(jwtToken, tokenValidationParameters, out validatedToken);
            if (validatedToken == null)
            {
                validatedClaims = null;
                return false;
            }

            var claims = principal.Claims.ToDictionary(c => c.Type, c => c.Value);
            foreach (var requiredClaim in requiredClaims)
            {
                if (!claims.ContainsKey(requiredClaim.Key) || claims[requiredClaim.Key] != requiredClaim.Value)
                {
                    validatedClaims = null;
                    return false;
                }
            }

            validatedClaims = claims;
            return true;
        }
    }
}
