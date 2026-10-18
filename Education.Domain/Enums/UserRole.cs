namespace Education.Domain.Enums
{
    /// <summary>
    /// Enum роли пользователя
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Администратор
        /// </summary>
        Admin,

        /// <summary>
        /// Модератор
        /// </summary>
        Moderator,

        /// <summary>
        /// Создатель курсов
        /// </summary>
        CourseCreator,

        /// <summary>
        /// Обычный пользователь
        /// </summary>
        User,
    }
}
