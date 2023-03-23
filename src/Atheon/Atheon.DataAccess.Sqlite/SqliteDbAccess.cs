using Dapper;
using Microsoft.Extensions.Logging;

namespace Atheon.DataAccess.Sqlite;

public class SqliteDbAccess : IDbAccess
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<SqliteDbAccess> _logger;

    public SqliteDbAccess(IDbConnectionFactory dbConnectionFactory,
        ILogger<SqliteDbAccess> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _dbConnectionFactory.GetDbConnection();
            await connection.ExecuteAsync(new CommandDefinition(query, parameters: parameters, cancellationToken: cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(ExecuteAsync));
        }
    }

    public async Task<List<T>> QueryAsync<T>(
        string query,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _dbConnectionFactory.GetDbConnection();
            var result = await connection.QueryAsync<T>(new CommandDefinition(query, parameters: parameters, cancellationToken: cancellationToken));
            return (result as List<T>)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(QueryAsync));
            throw;
        }
    }

    public async Task<T> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _dbConnectionFactory.GetDbConnection();
            var result = await connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(query, parameters: parameters, cancellationToken: cancellationToken));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {MethodName}", nameof(QueryFirstOrDefaultAsync));
            throw;
        }
    }
}
