using StackExchange.Redis;
namespace TokenService.Service
{
    public class RedisService
    {
        public readonly IDatabase db;
        private readonly ConnectionMultiplexer redis;

        public RedisService(string connectionString)
        {
            this.redis = ConnectionMultiplexer.Connect(connectionString);
            this.db = redis.GetDatabase();
        }
    }
}
