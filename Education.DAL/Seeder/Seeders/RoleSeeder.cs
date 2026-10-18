using Education.DAL.Seeder.Interfaces;
using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Education.DAL.Seeder.Seeders
{
    public class RoleSeeder : IRoleSeeder
    {
        private readonly RoleManager<AppRoleEntity> _roleManager;

        public RoleSeeder(RoleManager<AppRoleEntity> roleManager)
        {
            _roleManager = roleManager;
        }

        /// <summary>
        /// Ожидаемый набор ролей
        /// </summary>
        private static readonly List<(UserRole Role, string Description, decimal Price)> ExpectedRoles = new()
        {
            (
                UserRole.Admin,
                "Администратор платформы",
                0m
            ),

            (
                UserRole.Moderator,
                "Модератор платформы",
                0m
            ),

            (
                UserRole.CourseCreator,
                "Человек, у которого есть права на создание курсов",
                120_000.00m
            ),

            (
                UserRole.User,
                "Простой пользователь платформы, имеет доступ к своим курсам (может быть как учеником, так и учителем)",
                0m
            ),
        };

        public async Task SeedRoleAsync()
        {
            // Получаем все роли из БД
            var allRoles = await _roleManager.Roles
                .AsNoTracking()
                .Select(r => r.Name!)
                .ToListAsync();

            // Если в БД вообще еще нет данных, то просто ее заполняем
            if (!allRoles.Any())
            {
                await SeedMissingRolesAsync(ExpectedRoles);
                return;
            }

            // Проверка лишних ролей (это критическая ошибка)
            var extraRoles = GetExtraRoles(allRoles);

            if (extraRoles.Any())
            {
                var message =
                    $"Обнаружены лишние роли в БД: {string.Join(", ", extraRoles)}. " +
                    $"Ожидаемые роли: {string.Join(", ", ExpectedRoles.Select(r => r.Role.RoleToDbString()))}";

                throw new InvalidOperationException(message);
            }

            // Проверка недостающих ролей
            var missingRoles = GetMissingRoles(allRoles);

            if (!missingRoles.Any())
                return;

            await SeedMissingRolesAsync(missingRoles);
        }

        /// <summary>
        /// Получить список лишних ролей (есть в БД, но нет в ожидаемых)
        /// </summary>
        /// <param name="existingRoles">Роли, существующие в БД</param>
        /// <returns>Список имен лишних ролей</returns>
        private static List<string> GetExtraRoles(IEnumerable<string> existingRoles)
        {
            // Приводим все роли из Enum в string
            var allStringRoles = ExpectedRoles
                .Select(r => r.Role.RoleToDbString())
                .ToHashSet();

            return existingRoles
                .Where(name => !allStringRoles.Contains(name))
                .ToList();
        }

        /// <summary>
        /// Получить список недостающих ролей (есть в ожидаемых, но нет в БД)
        /// </summary>
        /// <param name="existingRoles">Роли, существующие в БД</param>
        /// <returns>Список ожидаемых ролей, которых нет в БД</returns>
        private static List<(UserRole Role, string Description, decimal Price)> GetMissingRoles(
            IEnumerable<string> existingRoles)
        {
            var existingNames = existingRoles.ToHashSet();

            return ExpectedRoles
                .Where(r => !existingNames.Contains(r.Role.RoleToDbString()))
                .ToList();
        }

        /// <summary>
        /// Создать недостающие роли в БД
        /// </summary>
        /// <param name="missingRoles">Список ролей для создания</param>
        /// <returns>Результат операции</returns>
        private async Task SeedMissingRolesAsync(
            IEnumerable<(UserRole Role, string Description, decimal Price)> missingRoles)
        {
            foreach (var (role, description, price) in missingRoles)
            {
                var roleName = role.RoleToDbString();

                var roleEntity = new AppRoleEntity
                {
                    Name = roleName,
                    RoleDescription = description,
                    Price = price,
                };

                var result = await _roleManager.CreateAsync(roleEntity);

                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException($"Не удалось создать роль '{roleName}': {errors}");
                }
            }
        }
    }
}
