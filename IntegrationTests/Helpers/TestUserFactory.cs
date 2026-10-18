using Education.Domain.DTO.Request.Auth;

namespace IntegrationTests.Helpers
{
    public sealed record TestUserData(
        string Email,
        string Login,
        string Password,
        string FirstName,
        string LastName)
    {
        public static TestUserData New(string? password = null)
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            return new(
                Email: $"user_{suffix}@test.com",
                Login: $"login_{suffix}",
                Password: password ?? "Password1!",
                FirstName: "Ivan",
                LastName: "Petrov");
        }

        public RegisterRequest ToRegisterRequest() => new()
        {
            Email = Email,
            Login = Login,
            Password = Password,
            ConfirmPassword = Password,
            FirstName = FirstName,
            LastName = LastName,
        };
    }
}
