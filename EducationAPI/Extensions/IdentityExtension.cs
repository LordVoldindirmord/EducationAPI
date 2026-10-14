using Education.DAL.Context;
using Education.Domain.Entity;
using Microsoft.AspNetCore.Identity;

namespace EducationAPI.Extensions
{
    /// <summary>
    /// Настройка Identity
    /// </summary>
    public static class IdentityExtension
    {
        /// <summary>
        /// Добавить Identity в middleware
        /// </summary>
        public static void AddIdentity(this IServiceCollection services)
        {
            services.AddDataProtection();

            var dataProtectionProviderType = typeof(DataProtectorTokenProvider<>)
                .MakeGenericType(typeof(UserEntity));
            services.AddTransient(dataProtectionProviderType);

            services.AddCookie();

            services.AddIdentityCore<UserEntity>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<AppRoleEntity>()
            .AddSignInManager()
            .AddEntityFrameworkStores<EducationDbContext>()
            .AddDefaultTokenProviders();
        }

        /// <summary>
        /// Добавить работу с Cookie
        /// </summary>
        /// <param name="services"></param>
        private static void AddCookie(this IServiceCollection services)
        {
            services
                .AddAuthentication(IdentityConstants.ApplicationScheme)
                .AddCookie(IdentityConstants.ApplicationScheme, options =>
                {
                    options.Cookie.Name = "EducationPlatform";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;

                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    };
                });
        }
    }
}
