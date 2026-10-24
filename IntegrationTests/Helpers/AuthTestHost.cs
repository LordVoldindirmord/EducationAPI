using Education.DAL.Context;
using Education.DAL.Seeder.Seeders;
using Education.Domain.Entity;
using Education.Service.Auth.Implementations;
using Education.Service.Auth.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.Helpers
{
    public sealed class AuthTestHost : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        public FakeEmailService Email { get; }

        public FakeSignInService SignIn { get; }

        public IAuthService Auth => _provider.GetRequiredService<IAuthService>();

        public UserManager<UserEntity> Users => _provider.GetRequiredService<UserManager<UserEntity>>();

        public RoleManager<AppRoleEntity> Roles => _provider.GetRequiredService<RoleManager<AppRoleEntity>>();

        private AuthTestHost(ServiceProvider provider, FakeEmailService email, FakeSignInService signIn)
        {
            _provider = provider;
            Email = email;
            SignIn = signIn;
        }

        public static async Task InitializeDatabaseAsync(string connectionString)
        {
            await using var host = Create(connectionString);
            await using var scope = host._provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<EducationDbContext>();
            await db.Database.MigrateAsync();

            var seeder = new RoleSeeder(scope.ServiceProvider.GetRequiredService<RoleManager<AppRoleEntity>>());
            await seeder.SeedRoleAsync();
        }

        public static AuthTestHost Create(string connectionString)
        {
            var email = new FakeEmailService();
            var signIn = new FakeSignInService();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://frontend.test",
                    ["Frontend:ConfirmEmailPath"] = "/confirm-email",
                    ["Frontend:ResetPasswordPath"] = "/reset-password",
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            services.AddDataProtection();

            services.AddDbContext<EducationDbContext>(options => options.UseNpgsql(connectionString));

            services.AddIdentityCore<UserEntity>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.User.RequireUniqueEmail = true;
                })
                .AddRoles<AppRoleEntity>()
                .AddEntityFrameworkStores<EducationDbContext>()
                .AddDefaultTokenProviders();

            services.AddSingleton<IEmailService>(email);
            services.AddSingleton<ISignInService>(signIn);
            services.AddScoped<ILinkGenerator, Education.Service.Auth.Implementations.LinkGenerator>();
            services.AddScoped<IEmailTemplateBuilder, EmailTemplateBuilder>();
            services.AddScoped<IAuthService, AuthService>();

            return new AuthTestHost(services.BuildServiceProvider(), email, signIn);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
        }
    }
}