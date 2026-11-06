using System;
using System.Data;

namespace AutoCare.Infrastructure.Persistence
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public class DatabaseOptions
    {
        public string ConnectionString { get; set; }
        public int CommandTimeoutSeconds { get; set; } = 30;
    }
}
