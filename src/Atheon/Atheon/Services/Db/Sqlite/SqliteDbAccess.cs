using Atheon.Services.Interfaces;
using Dapper;

namespace Atheon.Services.Db.Sqlite;

public class SqliteDbAccess : IDbAccess
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public SqliteDbAccess(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task ExecuteAsync(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionFactory.GetDbConnection();
        await connection.ExecuteAsync(new CommandDefinition(query, parameters: parameters, cancellationToken: cancellationToken));
    }

    public async Task<List<T>> QueryAsync<T>(
        string query,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionFactory.GetDbConnection();
        var result = await connection.QueryAsync<T>(new CommandDefinition(query, parameters: parameters, cancellationToken: cancellationToken));
        return (result as List<T>)!;
    }

    public async Task<T> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default)
    {
        using var connection = _dbConnectionFactory.GetDbConnection();
        var result = await connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(query, parameters: parameters, cancellationToken: cancellationToken));
        return result;
    }
}
