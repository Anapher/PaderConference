using StackExchange.Redis.Extensions.Core.Configuration;

namespace Strive.Config
{
    public class KeyValueDatabaseConfig
    {
        public bool UseInMemory { get; set; }

        public RedisConfiguration? Redis { get; set; }
    }
}
