using Education.Service.Auth.Interfaces;
using Education.Service.Purchases.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Education.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IntegrationTests.Helpers;

// Runs the actual Program, routing, filters, Identity cookies, services and repositories.
public sealed class ApiTestFactory(string connectionString) : WebApplicationFactory<Program>
{
    public FakeEmailService Email { get; } = new();
    public FakePaymentService Payments { get; } = new();
    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "education-http-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Frontend:BaseUrl"] = "https://frontend.test",
            ["Frontend:ConfirmEmailPath"] = "/confirm-email",
            ["Frontend:ResetPasswordPath"] = "/reset-password",
            ["FileStorage:RootPath"] = StorageRoot,
        }));
        builder.ConfigureTestServices(services =>
        {
            // Explicitly replace the captured DbContext options as well as configuration.
            services.RemoveAll<DbContextOptions<EducationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<EducationDbContext>>();
            services.AddDbContext<EducationDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Email);
            services.RemoveAll<IPaymentService>();
            services.AddSingleton<IPaymentService>(Payments);
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            // The default key-ring warmup still reads the user's persistent keys.
            var warmups = services.Where(d => d.ServiceType == typeof(IHostedService)
                && d.ImplementationType?.Namespace?.StartsWith("Microsoft.AspNetCore.DataProtection") == true).ToArray();
            foreach (var warmup in warmups) services.Remove(warmup);
        });
    }

    public HttpClient CreateApiClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }
}
