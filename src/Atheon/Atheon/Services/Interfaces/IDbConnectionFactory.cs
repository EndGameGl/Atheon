using System.Data;

namespace Atheon.Services.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection GetDbConnection();
}
