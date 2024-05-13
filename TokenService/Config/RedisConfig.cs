namespace TokenService.Config
{
    public class RedisConfig
    {
        public bool EnableRedis { get; set; } = false;

        public string ConnectionString { get; set; } = string.Empty;

        public string ValidRefreshTokenIdKeyName { get; set; } = "ValidRefreshTokenId";
    }
}
