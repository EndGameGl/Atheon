using System.Text.Json.Serialization;

namespace Atheon.Models.Api;

public class ApiResponse<T>
{
    [JsonPropertyName("Data")]
    public T? Data { get; set; }

    [JsonPropertyName("Message")]
    public string? Message { get; set; }

    [JsonPropertyName("Code")]
    public ApiResponseCode Code { get; set; }

    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T>
        {
            Data = data,
            Code = ApiResponseCode.Ok,
        };
    }

    public static ApiResponse<T> Error(Exception exception)
    {
        return new ApiResponse<T>
        {
            Code = ApiResponseCode.InternalError,
            Message = exception.Message
        };
    }
}
