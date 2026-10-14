using Education.Domain.Enums;
using Education.Domain.ResponseModel;
using Microsoft.AspNetCore.Mvc;

namespace EducationAPI.Extensions
{
    /// <summary>
    /// Расширение для контроллера
    /// </summary>
    public static class ControllerExtension
    {
        /// <summary>
        /// Преобразует BaseResponse в соответствующий HTTP-ответ
        /// </summary>
        public static IActionResult ToActionResult<T>(this ControllerBase controller, InternalResponse<T> response)
        {
            return response.StatusCode switch
            {
                StatusCode.OK => controller.Ok(response.GetApiResponse()),
                StatusCode.Created => controller.Created(string.Empty, response.GetApiResponse()),
                StatusCode.NoContent => controller.NoContent(),
                StatusCode.BadRequest => controller.BadRequest(response.GetApiResponse()),
                StatusCode.Unauthorized => controller.Unauthorized(response.GetApiResponse()),
                StatusCode.Forbidden => controller.StatusCode(403, response.GetApiResponse()),
                StatusCode.NotFound => controller.NotFound(response.GetApiResponse()),
                StatusCode.Conflict => controller.Conflict(response.GetApiResponse()),
                StatusCode.TooManyRequests => controller.StatusCode(429, response.GetApiResponse()),
                StatusCode.InternalServerError => controller.StatusCode(500, response.GetApiResponse()),
                _ => controller.StatusCode(500, response.GetApiResponse())
            };
        }
    }
}
