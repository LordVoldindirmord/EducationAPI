using Education.DAL.Interfaces;
using Education.Domain.Enums;
using Education.Service.Courses.Interfaces;

namespace Education.Service.Courses.Implementations
{
    public class CourseMembershipService : ICourseMembershipService
    {
        private readonly ICourseMemberRepository _courseMemberRepository;

        public CourseMembershipService(ICourseMemberRepository courseMemberRepository)
        {
            _courseMemberRepository = courseMemberRepository;
        }

        public async Task<CourseRole?> GetRoleAsync(Guid userId, Guid courseId)
        {
            var member = await _courseMemberRepository.GetByUserAndCourseAsync(userId, courseId);

            return member?.Role;
        }

        public async Task<bool> HasAnyRoleAsync(Guid userId, Guid courseId, IEnumerable<CourseRole> roles)
        {
            var member = await _courseMemberRepository.GetByUserAndCourseAsync(userId, courseId);

            if (member == null)
                return false;

            return roles.Contains(member.Role);
        }

        public async Task<bool> IsMemberAsync(Guid userId, Guid courseId)
        {
            return await _courseMemberRepository.ExistsAsync(userId, courseId);
        }
    }
}
