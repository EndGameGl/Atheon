using Dapper;
using System.Data;
using System.Text.Json;

namespace Atheon.DataAccess
{
    public class JsonTypeHandler<THandledType> : SqlMapper.ITypeHandler
    {
        private readonly JsonSerializerOptions _serializerOptions;

        public JsonTypeHandler(
            JsonSerializerOptions serializerOptions)
        {
            _serializerOptions = serializerOptions;
        }

        public object Parse(
            Type destinationType,
            object value)
        {
            return JsonSerializer.Deserialize<THandledType>((string)value, _serializerOptions);
        }

        public void SetValue(IDbDataParameter parameter, object value)
        {
            parameter.Value = value == DBNull.Value ? value : JsonSerializer.Serialize((THandledType)value, _serializerOptions);
        }

    }
}
