using Education.DAL.Interfaces;
using Education.Domain.Enums;
using Education.Service.Courses.Interfaces;

namespace Education.Service.Courses.Implementations
{
    public class CourseAccessService : ICourseAccessService
    {
        private readonly ICourseMembershipService _membershipService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly ICourseRepository _courseRepository;

        public CourseAccessService(
            ICourseMembershipService courseMembershipService,
            IEnrollmentService enrollmentService,
            ICourseRepository courseRepository)
        {
            _membershipService = courseMembershipService;
            _enrollmentService = enrollmentService;
            _courseRepository = courseRepository;
        }

        public async Task<bool> CanManageAsync(Guid userId, Guid courseId)
        {
            var access = await GetAccessTypeAsync(userId, courseId);

            // Manager и Owner могут управлять, Teacher — нет
            return access >= CourseAccessType.Manager;
        }

        public async Task<bool> CanViewAsync(Guid userId, Guid courseId)
        {
            var access = await GetAccessTypeAsync(userId, courseId);

            // Управляющие видят всё, включая неопубликованное
            if (access >= CourseAccessType.Teacher)
                return true;

            // Студент видит только опубликованный курс
            if (access == CourseAccessType.Student)
                return await _courseRepository.IsPublishedAsync(courseId);

            return false;
        }

        public async Task<CourseAccessType> GetAccessTypeAsync(Guid userId, Guid courseId)
        {
            // Проверка управляющих ролей в курсе
            var role = await _membershipService.GetRoleAsync(userId, courseId);

            if (role != null)
            {
                return role.Value switch
                {
                    CourseRole.Owner => CourseAccessType.Owner,
                    CourseRole.Manager => CourseAccessType.Manager,
                    CourseRole.Teacher => CourseAccessType.Teacher,
                    _ => CourseAccessType.None,
                };
            }

            // Проверка записи как студент
            if (await _enrollmentService.IsEnrolledAsync(userId, courseId))
                return CourseAccessType.Student;

            return CourseAccessType.None;
        }

        public async Task<bool> HasAccessAsync(Guid userId, Guid courseId)
        {
            return await GetAccessTypeAsync(userId, courseId) != CourseAccessType.None;
        }
    }
}