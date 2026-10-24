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
                UserRole.CourseCreator => "course_creator",
                UserRole.User => "user",
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Неизвестный тип роли"),
            };
        }

        public static UserRole DbStringToRole(this string value)
        {
            return value switch
            {
                "admin" => UserRole.Admin,
                "moderator" => UserRole.Moderator,
                "course_creator" => UserRole.CourseCreator,
                "user" => UserRole.User,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Неизвестный тип роли"),
            };
        }
    }
}