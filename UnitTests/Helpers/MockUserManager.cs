using Education.Domain.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests.Helpers
{
    public static class MockUserManager
    {
        public static Mock<UserManager<UserEntity>> Create()
        {
            var store = new Mock<IUserStore<UserEntity>>();     // Хранилище пользователей

            var mgr = new Mock<UserManager<UserEntity>>(
                store.Object,
                new Mock<IOptions<IdentityOptions>>().Object,   // Mock настройка Identity
                new Mock<IPasswordHasher<UserEntity>>().Object, // Mock хеширование пароля
                Array.Empty<IUserValidator<UserEntity>>(),      // Валидаторы (пустые массивы)
                Array.Empty<IPasswordValidator<UserEntity>>(),
                new Mock<ILookupNormalizer>().Object,           // Mock нормализацие Email и UserName
                new Mock<IdentityErrorDescriber>().Object,      // Mock описание ошибок
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<UserEntity>>>().Object);

            return mgr;
        }
    }
}