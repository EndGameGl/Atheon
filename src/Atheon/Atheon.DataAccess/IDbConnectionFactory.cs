using System.Data;

namespace Atheon.DataAccess;

public interface IDbConnectionFactory
{
    IDbConnection GetDbConnection();
}
