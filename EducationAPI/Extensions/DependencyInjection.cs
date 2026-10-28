using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.DAL.Repositories;
using Education.DAL.Seeder.Interfaces;
using Education.DAL.Seeder.Seeders;
using Education.Domain.InternalServiceModels.Purchases;
using Education.Service.Auth.Implementations;
using Education.Service.Auth.Interfaces;
using Education.Service.Common.Implementations;
using Education.Service.Common.Interfaces;
using Education.Service.Courses.Implementations;
using Education.Service.Courses.Interfaces;
using Education.Service.Files.Implementations;
using Education.Service.Files.Interfaces;
using Education.Service.Purchases.Implementations;
using Education.Service.Purchases.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EducationAPI.Extensions
{
    /// <summary>
    /// Настройка DI контейнера
    /// </summary>
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
            services.AddServices(configuration);

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
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<ICourseRepository, CourseRepository>();
            services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<IModuleRepository, ModuleRepository>();
            services.AddScoped<IQuizLessonRepository, QuizLessonRepository>();
            services.AddScoped<ITestQuestionRepository, TestQuestionRepository>();
            services.AddScoped<ITextLessonRepository, TextLessonRepository>();
            services.AddScoped<IUserProgressRepository, UserProgressRepository>();
            services.AddScoped<IVideoLessonRepository, VideoLessonRepository>();
        }

        private static void AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Auth
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailTemplateBuilder, EmailTemplateBuilder>();
            services.AddScoped<ILinkGenerator, Education.Service.Auth.Implementations.LinkGenerator>();
            services.AddScoped<ISignInService, SignInService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Common
            services.AddScoped<IPermissionService, PermissionService>();

            // Files
            services.AddScoped<IFileManager>(provider =>
            {
                var environment = provider.GetRequiredService<IHostEnvironment>();

                // По умолчанию берем родительскую папку EducationAPI
                // При необходимости путь можно задать через FileStorage:RootPath.
                var rootPath = Path.GetFullPath(
                    configuration["FileStorage:RootPath"] ?? "..",
                    environment.ContentRootPath);

                // Размеры задаются в байтах. При отсутствии значения берем стандартный лимит.
                var maxImageSize = configuration.GetValue<long?>("FileStorage:MaxImageSize")
                    ?? FileManager.DEFAULT_MAX_IMAGE_SIZE;
                var maxVideoSize = configuration.GetValue<long?>("FileStorage:MaxVideoSize")
                    ?? FileManager.DEFAULT_MAX_VIDEO_SIZE;

                return new FileManager(rootPath, maxImageSize, maxVideoSize);
            });

            // Courses
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICourseAccessService, CourseAccessService>();
            services.AddScoped<ICourseMembershipService, CourseMembershipService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IEnrollmentService, EnrollmentService>();

            // Purchases
            services.AddScoped<ICoursePurchaseHandler, CoursePurchaseHandler>();
            services.AddScoped<IPaymentService, YooKassaPaymentService>();
            services.AddScoped<IPurchaseService, PurchaseService>();
            services.AddScoped<IRolePurchaseHandler, RolePurchaseHandler>();
            services.Configure<YooKassaOptions>(configuration.GetSection("YooKassa"));

            services.AddHttpContextAccessor();
        }

        private static void AddSeeder(this IServiceCollection services)
        {
            services.AddScoped<IRoleSeeder, RoleSeeder>();
            services.AddScoped<ISeeder, Seeder>();
        }
    }
}
