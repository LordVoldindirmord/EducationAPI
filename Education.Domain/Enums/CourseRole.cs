namespace Education.Domain.Enums
{
    /// <summary>
    /// <list type="bullet">
    ///     <item><see cref="CourseRole.Owner"/> — владелец курса, полный доступ</item>
    ///     <item><see cref="CourseRole.Manager"/> — менеджер курса, редактирование контента</item>
    ///     <item><see cref="CourseRole.Teacher"/> — учитель курса, ведение уроков</item>
    /// </list>
    /// </summary>
    public enum CourseRole
    {
        /// <summary>
        /// Владелец курса
        /// </summary>
        Owner,

        /// <summary>
        /// Менеджер курса
        /// </summary>
        Manager,

        /// <summary>
        /// Учитель курса
        /// </summary>
        Teacher,
    }
}
