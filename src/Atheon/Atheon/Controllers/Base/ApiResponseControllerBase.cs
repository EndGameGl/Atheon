using Atheon.Models.Api;
using Microsoft.AspNetCore.Mvc;

namespace Atheon.Controllers.Base
{
    public class ApiResponseControllerBase : ControllerBase
    {
        protected ILogger Logger { get; }

        protected ApiResponseControllerBase(
            ILogger logger)
        {
            Logger = logger;
        }

        protected IActionResult CustomResult<T>(T data, ApiResponseCode code)
        {
            return new ObjectResult(new ApiResponse<T>()
            {
                Data = data,
                Code = code
            });
        }

        protected IActionResult OkResult<T>(T data)
        {
            return new ObjectResult(ApiResponse<T>.Ok(data));
        }

        protected IActionResult ErrorResult(Exception exception)
        {
            return new ObjectResult(ApiResponse<int>.Error(exception));
        }
    }
}
