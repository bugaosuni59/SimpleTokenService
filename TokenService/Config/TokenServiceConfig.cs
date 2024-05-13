namespace TokenService.Config
{
    public class TokenServiceConfig
    {
        /// <summary>
        /// Get or set the key used to allow other services to communicate with this service
        /// </summary>
        public string S2S_KEY { get; set; } = string.Empty;

        /// <summary>
        /// Get or set the key used to sign the JWT token
        /// </summary>
        public string SIGN_TOKEN_KEY { get; set; } = string.Empty;

        /// <summary>
        /// For the S2S_KEY and TOKEN_KEY, use environment variables first.
        /// True to load keys from environment variables dynamically.
        /// </summary>
        public bool UseEnvironmentVariablesFirst { get; set; } = false;

        /// <summary>
        /// Get or set the default access token expire minutes, optional.
        /// </summary>
        public int DefaultAccessTokenExpireMinutes { get; set; } = 30;

        /// <summary>
        /// Get or set the default refresh token expire hours, optional.
        /// </summary>
        public int DefaultRefreshTokenExpireHours { get; set; } = 24 * 7;

        /// <summary>
        /// Get or set the Redis configuration, optional.
        /// </summary>
        public RedisConfig RedisConfig { get; set; } = new RedisConfig();
    }
}
