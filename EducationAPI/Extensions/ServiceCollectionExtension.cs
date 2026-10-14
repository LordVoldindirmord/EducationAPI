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
        /// Добавить контроллеры с настройкой JSON (enum как строки)
        /// </summary>
        public static IServiceCollection AddControllersWithJson(this IServiceCollection services)
        {
            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
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
