using Education.Domain.DTO.Request.Courses.Courses;
using Education.Service.Auth.Interfaces;
using Education.Service.Courses.Interfaces;
using EducationAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EducationAPI.Controllers.Web
{
    [Route("api/course")]
    [ApiController]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ICurrentUserService _currentUser;

        public CourseController(
            ICourseService courseService,
            ICurrentUserService currentUser)
        {
            _courseService = courseService;
            _currentUser = currentUser;
        }

        [HttpPost("create")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
        [Authorize(Policy = AuthorizationExtension.ADMIN_OR_MODERATOR_OR_COURSE_CREATOR)]
        public async Task<IActionResult> Create([FromForm] CreateCourseRequest request)
        {
            var userId = _currentUser.UserId;

            var response = await _courseService.CreateCourseAsync(request, userId!.Value);

            return this.ToActionResult(response);
        }

        [HttpPut("update")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
        [Authorize(Policy = AuthorizationExtension.ADMIN_OR_MODERATOR_OR_COURSE_CREATOR)]
        public async Task<IActionResult> Update([FromForm] UpdateCourseRequest request)
        {
            var userId = _currentUser.UserId;

            var response = await _courseService.UpdateCourseAsync(request, userId!.Value);
            
            return this.ToActionResult(response);
        }

        [HttpDelete("delete")]
        [Authorize(Policy = AuthorizationExtension.ADMIN_OR_MODERATOR_OR_COURSE_CREATOR)]
        public async Task<IActionResult> Delete([FromBody] DeleteCourseRequest request)
        {
            var userId = _currentUser.UserId;

            var response = await _courseService.DeleteCourseAsync(request, userId!.Value);

            return this.ToActionResult(response);
        }

        [HttpPut("publish")]
        [Authorize(Policy = AuthorizationExtension.ADMIN_OR_MODERATOR_OR_COURSE_CREATOR)]
        public async Task<IActionResult> Publish([FromBody] PublishCourseRequest request)
        {
            var userId = _currentUser.UserId;

            var response = await _courseService.PublishCourseAsync(request, userId!.Value);

            return this.ToActionResult(response);
        }
    }
}
