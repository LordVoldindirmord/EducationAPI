using System;
using System.Collections.Generic;
using System.Text;

namespace Education.Domain.Enums
{
    /// <summary>
    /// Тип доступа пользователя к курсу.
    /// Значения упорядочены от минимального к максимальному,
    /// чтобы можно было сравнивать: <c>access >= <see cref="Manager"/></c>.
    /// </summary>
    public enum CourseAccessType
    {
        /// <summary>
        /// Доступа нет (не управляющий и не записан).
        /// </summary>
        None = 0,

        /// <summary>
        /// Студент курса (записан через Enrollment).
        /// Может смотреть контент, проходить уроки.
        /// </summary>
        Student = 1,

        /// <summary>
        /// Учитель курса (CourseMember.Teacher).
        /// Может вести уроки.
        /// </summary>
        Teacher = 2,

        /// <summary>
        /// Менеджер курса (CourseMember.Manager).
        /// Может редактировать контент и метаданные.
        /// </summary>
        Manager = 3,

        /// <summary>
        /// Владелец курса (CourseMember.Owner).
        /// Полный доступ.
        /// </summary>
        Owner = 4,
    }
}