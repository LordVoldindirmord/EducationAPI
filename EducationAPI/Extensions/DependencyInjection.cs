using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.DAL.Repositories;
using Education.DAL.Seeder.Interfaces;
using Education.DAL.Seeder.Seeders;
using Education.Service.Auth.Implementations;
using Education.Service.Auth.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EducationAPI.Extensions
{
    public static class DependencyInjection
    {
        private const string CONNECTION_STRING_NAME = "DefaultConnection";

        /// <summary>
        /// DI контейнер
        /// </summary>
        public static IServiceCollection AddEducationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Добавление DbContext
            services.AddDbContext(configuration);

            // Добавление репозиториев
            services.AddRepositories();

            // Добавление сервисов
            services.AddServices();

            // Регистрация Seeder для инициализации БД начальными значениями
            services.AddSeeder();

            return services;
        }

        private static void AddDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(CONNECTION_STRING_NAME)
                ?? throw new InvalidOperationException($"Connection string '{CONNECTION_STRING_NAME}' not found");

            services.AddDbContext<EducationDbContext>(options => options.UseNpgsql(connectionString));
        }

        private static void AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICourseMemberRepository, CourseMemberRepository>();
            services.AddScoped<ICoursePurchaseRepository, CoursePurchaseRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<IModuleRepository, ModuleRepository>();
            services.AddScoped<IQuizLessonRepository, QuizLessonRepository>();
            services.AddScoped<IRolePurchaseRepository, RolePurchaseRepository>();
            services.AddScoped<ITestQuestionRepository, TestQuestionRepository>();
            services.AddScoped<ITextLessonRepository, TextLessonRepository>();
            services.AddScoped<IUserProgressRepository, UserProgressRepository>();
            services.AddScoped<IVideoLessonRepository, VideoLessonRepository>();
        }

        private static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailTemplateBuilder, EmailTemplateBuilder>();
            services.AddScoped<ILinkGenerator, Education.Service.Auth.Implementations.LinkGenerator>();
            services.AddScoped<ISignInService, SignInService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddHttpContextAccessor();
        }

        private static void AddSeeder(this IServiceCollection services)
        {
            services.AddScoped<IRoleSeeder, RoleSeeder>();
            services.AddScoped<ISeeder, Seeder>();
        }
    }
}