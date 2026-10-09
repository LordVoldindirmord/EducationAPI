namespace Education.DAL.Seeder.Interfaces
{
    /// <summary>
    /// Инициализация всех сидеров
    /// </summary>
    public interface ISeeder
    {
        /// <summary>
        /// Инициализировать все сидеры
        /// </summary>
        Task SeedAsync();
    }
}
