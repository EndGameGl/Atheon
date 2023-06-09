using Dapper;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Atheon.DataAccess.Sqlite;

public class SqliteSettingsStorage : ISettingsStorage
{
	private readonly ILogger<SqliteSettingsStorage> _logger;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly IDbConnectionFactory _dbConnectionFactory;

	public SqliteSettingsStorage(
		ILogger<SqliteSettingsStorage> logger,
		IOptions<JsonOptions> jsonOptions,
		IDbConnectionFactory dbConnectionFactory
	)
	{
		_logger = logger;
		_jsonOptions = jsonOptions.Value.SerializerOptions;
		_dbConnectionFactory = dbConnectionFactory;
	}

	public async Task<T?> GetOption<T>(string key, Func<T>? defaultValue = null)
	{
		using var connection = _dbConnectionFactory.GetDbConnection();

		var result = await connection.QueryFirstOrDefaultAsync<string?>(
			"SELECT Value FROM SettingsStorage WHERE Key = @Key",
			new { Key = key }
		);
		if (result is not null)
		{
			return JsonSerializer.Deserialize<T>(result, _jsonOptions);
		}
		if (defaultValue != null)
			return defaultValue();
		return default;
	}

	public async Task SetOption<T>(string key, T value)
	{
		using var connection = _dbConnectionFactory.GetDbConnection();

		await connection.ExecuteAsync(
			"INSERT INTO SettingsStorage(Key, Value) VALUES(@Key, @Value) ON CONFLICT (Key) DO UPDATE SET Value = @Value",
			new { Key = key, Value = JsonSerializer.Serialize(value, _jsonOptions) }
		);
	}
}
