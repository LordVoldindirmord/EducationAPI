using Education.Domain.DTO.Request.Courses.Courses;
using Education.Domain.ResponseModel;
using Education.Service.Courses.Interfaces;

namespace Education.Service.Courses.Implementations
{
    public class CourseService : ICourseService
    {
        public Task<InternalResponse<bool>> CreateCourseAsync(CreateCourseRequest request, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<InternalResponse<bool>> DeleteCourseAsync(DeleteCourseRequest request, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<InternalResponse<bool>> PublishCourseAsync(PublishCourseRequest request, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<InternalResponse<bool>> UpdateCourseAsync(UpdateCourseRequest request, Guid userId)
        {
            throw new NotImplementedException();
        }
    }
}
