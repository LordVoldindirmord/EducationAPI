using System;
using Education.Domain.Entity;

namespace UnitTests.Helpers
{
    public static class UserFactory
    {
        public static UserEntity Create(Guid? id = null, string email = "test@test.com", string userName = "testuser", bool emailConfirmed = true, string? firstName = "First", string? lastName = "Last")
        {
            return new UserEntity
            {
                Id = id ?? Guid.NewGuid(),
                Email = email,
                UserName = userName,
                FirstName = firstName!,
                LastName = lastName!,
                EmailConfirmed = emailConfirmed
            };
        }

        public static UserEntity Create(string email) => Create(email: email);
        public static UserEntity CreateForLogin(string userName) => Create(userName: userName);
        public static UserEntity CreateForEmail(string email, bool confirmed) => Create(email: email, emailConfirmed: confirmed);
    }
}
