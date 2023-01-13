namespace Atheon.Services.Interfaces;

public interface IDbAccess
{
    Task<List<T>> QueryAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default);
    Task<T> QueryFirstOrDefaultAsync<T>(string query, object? parameters = null, CancellationToken cancellationToken = default);
    Task ExecuteAsync(string query, object? parameters = null, CancellationToken cancellationToken = default);
}
