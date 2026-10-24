using Education.Domain.ResponseModel;

namespace Education.DAL.Seeder.Interfaces
{
    /// <summary>
    /// Сервис для одноразового заполнения ролей
    /// </summary>
    public interface IRoleSeeder
    {
        /// <summary>
        /// Создать базовые роли платформы, если их нет
        /// </summary>
        Task SeedRoleAsync();
    }
}