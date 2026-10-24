using Education.DAL.Seeder.Interfaces;

namespace EducationAPI.Extensions
{
    /// <summary>
    /// Содержит методы-расширения для <see cref="WebApplication"/>.
    /// </summary>
    public static class WebApplicationExtension
    {
        /// <summary>
        /// Запустить все сидеры при старте приложения
        /// </summary>
        public static async Task SeedDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var seeder = scope.ServiceProvider.GetRequiredService<ISeeder>();

            await seeder.SeedAsync();
        }
    }
}