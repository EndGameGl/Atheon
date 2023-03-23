namespace Atheon.DataAccess;

public interface IDbBootstrap
{
    Task InitialiseDb(CancellationToken cancellationToken);
}
