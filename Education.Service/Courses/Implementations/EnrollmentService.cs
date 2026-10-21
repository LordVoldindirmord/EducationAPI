using Education.DAL.Interfaces;
using Education.Domain.Entity;
using Education.Domain.ResponseModel;
using Education.Service.Courses.Interfaces;

namespace Education.Service.Courses.Implementations
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _enrollmentRepository;

        public EnrollmentService(IEnrollmentRepository enrollmentRepository)
        {
            _enrollmentRepository = enrollmentRepository;
        }

        public async Task<InternalResponse<bool>> EnrollAsync(Guid userId, Guid courseId)
        {
            // Проверка, что пользователь еще не записан на курс
            if (await _enrollmentRepository.ExistsAsync(userId, courseId))
                return CreatorResponse.Conflict<bool>("Пользователь уже записан на этот курс");

            // Создание курса
            var enrollment = new EnrollmentEntity()
            {
                UserId = userId,
                CourseId = courseId,
            };

            await _enrollmentRepository.CreateAsync(enrollment);
            await _enrollmentRepository.SaveChangesAsync();

            return CreatorResponse.Created(true, "Пользователь успешно записан на курс");
        }

        public async Task<bool> IsEnrolledAsync(Guid userId, Guid courseId)
        {
            return await _enrollmentRepository.ExistsAsync(userId, courseId);
        }

        public async Task<InternalResponse<bool>> UnenrollAsync(Guid userId, Guid courseId)
        {
            // Проверка, что запись на курс уже существует
            var enrollment = await _enrollmentRepository.GetByUserAndCourseTrackedAsync(userId, courseId);

            if (enrollment == null)
                return CreatorResponse.NotFound<bool>("Запись на курс не найдена");

            // Удаление записи
            _enrollmentRepository.Delete(enrollment);
            await _enrollmentRepository.SaveChangesAsync();

            return CreatorResponse.Ok(true, "Пользователь успешно отписан от курса");
        }
    }
}
