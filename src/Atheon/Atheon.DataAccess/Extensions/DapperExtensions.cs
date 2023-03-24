using Dapper;
using System.Text.Json;

namespace Atheon.DataAccess.Extensions;

public static class DapperExtensions
{
    public static void RegisterJsonHandler<THandledType>(JsonSerializerOptions jsonSerializerOptions)
    {
        SqlMapper.AddTypeHandler(typeof(THandledType), new JsonTypeHandler<THandledType>(jsonSerializerOptions));
    }
}
