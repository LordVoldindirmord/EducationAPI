using Education.Domain.Enums;

namespace Education.Domain.DTO.Response.Auth
{
    public record LoginResponse
    {
        public required Guid UserId { get; init; }

        /// <summary>
        /// Роль у пользователя может быть только одна
        /// </summary>
        public required UserRole Role { get; init; }

        public required string Email { get; init; }

        public required string Login { get; init; }
    }
}