using Education.Domain.ResponseModel;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EducationAPI.Extensions
{
    /// <summary>
    /// Расширения над IServiceCollection
    /// </summary>
    public static class ServiceCollectionExtension
    {
        public const string FRONTEND_POLICY_CORS_NAME = "AllowFrontend";

        /// <summary>
        /// Добавить контроллеры с валидацией ответа и настройкой JSON (enum как строки)
        /// </summary>
        public static IServiceCollection AddControllersWithJson(this IServiceCollection services)
        {
            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                });

            services.AddCustomInvalidModelStateResponse();

            return services;
        }

        /// <summary>
        /// Настроить единый формат ответа при провале валидации ModelState
        /// </summary>
        private static IServiceCollection AddCustomInvalidModelStateResponse(this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    var errors = actionContext.ModelState
                        .Where(kvp => kvp.Value?.Errors.Count > 0)
                        .SelectMany(kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage));

                    var response = new ApiResponse<object>
                    {
                        Description = string.Join("; ", errors),
                        Data = null,
                        IsSuccess = false,
                    };

                    return new BadRequestObjectResult(response);
                };
            });

            return services;
        }

        /// <summary>
        /// Добавить CORS для фронтенда
        /// </summary>
        public static IServiceCollection AddFrontendCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(FRONTEND_POLICY_CORS_NAME, policy =>
                {
                    policy
                        .WithOrigins("http://localhost:3000", "https://localhost:3001")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();

                });
            });

            return services;
        }
    }
}
