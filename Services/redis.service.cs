using StackExchange.Redis;

public class RedisService
{
    private readonly ConnectionMultiplexer _redis;

    public RedisService(IConfiguration configuration)
    {
        var redisConfig = configuration["Redis:Configuration"] ?? throw new InvalidOperationException("Redis configuration is missing.");
        _redis = ConnectionMultiplexer.Connect(redisConfig);
    }

    public IDatabase GetDatabase()
    {
        return _redis.GetDatabase();
    }
}