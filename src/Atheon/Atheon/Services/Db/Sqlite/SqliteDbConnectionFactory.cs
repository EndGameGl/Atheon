using Atheon.Options;
using Atheon.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Data;

namespace Atheon.Services.Db.Sqlite
{
    public class SqliteDbConnectionFactory : IDbConnectionFactory
    {
        private readonly ILogger<SqliteDbConnectionFactory> _logger;
        private readonly IOptions<DatabaseOptions> _options;

        public SqliteDbConnectionFactory(
            ILogger<SqliteDbConnectionFactory> logger,
            IOptions<DatabaseOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        public IDbConnection GetDbConnection()
        {
            var options = _options.Value.Databases["Sqlite"];
            return new SqliteConnection(options.ConnectionString);
        }
    }
}
