namespace Atheon.Services.Interfaces;

public interface IDbBootstrap
{
    Task InitialiseDb(CancellationToken cancellationToken);
}
