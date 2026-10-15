using Education.Domain.Enums;
using Education.Domain.Extensions;

namespace EducationAPI.Extensions
{
    /// <summary>
    /// Настройка авторизации и политик доступа
    /// </summary>
    public static class AuthorizationExtension
    {
        /// <summary>
        /// Доступ только для администратора
        /// </summary>
        public const string ADMIN_ONLY = "AdminOnly";

        /// <summary>
        /// Доступ для администратора или модератора
        /// </summary>
        public const string ADMIN_OR_MODERATOR = "AdminOrModerator";

        /// <summary>
        /// Доступ для администратора, модератора или учителя
        /// </summary>
        public const string ADMIN_OR_MODERATOR_OR_TEACHER = "AdminOrModeratorOrTeacher";

        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Только Admin
                options.AddPolicy(ADMIN_ONLY, policy =>
                    policy.RequireRole(
                        UserRole.Admin.RoleToDbString()
                    )
                );

                // Admin или Moderator
                options.AddPolicy(ADMIN_OR_MODERATOR, policy =>
                    policy.RequireRole(
                        UserRole.Admin.RoleToDbString(),
                        UserRole.Moderator.RoleToDbString()
                    )
                );

                // Admin или Moderator или Teacher
                options.AddPolicy(ADMIN_OR_MODERATOR_OR_TEACHER, policy =>
                    policy.RequireRole(
                        UserRole.Admin.RoleToDbString(),
                        UserRole.Moderator.RoleToDbString(),
                        UserRole.Teacher.RoleToDbString()
                    )
                );
            });

            return services;
        }
    }
}
