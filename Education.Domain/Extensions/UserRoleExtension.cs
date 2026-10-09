using Education.Domain.Enums;

namespace Education.Domain.Extensions
{
    public static class UserRoleExtension
    {
        public static string RoleToDbString(this UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "admin",
                UserRole.Moderator => "moderator",
                UserRole.Teacher => "teacher",
                UserRole.Student => "student",
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Неизвестный тип роли"),
            };
        }

        public static UserRole DbStringToRole(this string value)
        {
            return value switch
            {
                "admin" => UserRole.Admin,
                "moderator" => UserRole.Moderator,
                "teacher" => UserRole.Teacher,
                "student" => UserRole.Student,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Неизвестный тип роли"),
            };
        }
    }
}
