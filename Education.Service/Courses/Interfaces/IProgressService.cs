using Education.Domain.DTO.Request.Courses.Progress;
using Education.Domain.DTO.Response.Courses.Progress;
using Education.Domain.ResponseModel;

namespace Education.Service.Courses.Interfaces
{
    /// <summary>
    /// Сервис для учета прогресса пользователей по урокам
    /// </summary>
    public interface IProgressService
    {
        /// <summary>
        /// Отметить видеоурок или текстовый урок пройденным.
        /// Повторный запрос не создает дополнительную запись прогресса.
        /// <para>
        /// Доступно записанным студентам и пользователям, имеющим доступ к курсу
        /// через <see cref="ICourseAccessService.HasAccessAsync"/>.
        /// Для тестового урока используется <see cref="SubmitQuizAsync"/>.
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя, который проходит урок</param>
        /// <param name="lessonId">Id урока</param>
        /// <returns>Флаг успешности операции</returns>
        Task<InternalResponse<bool>> CompleteLessonAsync(Guid userId, Guid lessonId);

        /// <summary>
        /// Проверить ответы теста на сервере и сохранить результат последней попытки.
        /// Вопрос засчитывается только при точном совпадении набора выбранных вариантов.
        /// Процент верных ответов вычисляется с отбрасыванием дробной части.
        /// <para>
        /// Доступно записанным студентам и пользователям, имеющим доступ к курсу
        /// через <see cref="ICourseAccessService.HasAccessAsync"/>.
        /// </para>
        /// </summary>
        /// <param name="userId">Id пользователя, который сдает тест</param>
        /// <param name="request">Id урока и выбранные варианты ответов</param>
        /// <returns>Оценка, результат прохождения и проходной балл</returns>
        Task<InternalResponse<QuizResultResponse>> SubmitQuizAsync(Guid userId, SubmitQuizRequest request);
    }
}
