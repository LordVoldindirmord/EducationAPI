using Education.Domain.DTO.Request.Courses.Lessons;
using Education.Service.Auth.Interfaces;
using Education.Service.Courses.Interfaces;
using EducationAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationAPI.Controllers.Web
{
    [Authorize]
    [Route("api/lesson")]
    [ApiController]
    public class LessonController : ControllerBase
    {
        private readonly ILessonService _lessonService;
        private readonly ICurrentUserService _currentUser;

        public LessonController(ILessonService lessonService, ICurrentUserService currentUser)
        {
            _lessonService = lessonService;
            _currentUser = currentUser;
        }

        [HttpPost("create")]
        [RequestSizeLimit(1024 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1024 * 1024 * 1024)]
        public async Task<IActionResult> Create([FromForm] CreateLessonRequest request)
        {
            var userId = _currentUser.UserId;
            var response = await _lessonService.CreateLessonAsync(request, userId!.Value);
            return this.ToActionResult(response);
        }
        [HttpPut("update")]
        [RequestSizeLimit(1024 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1024 * 1024 * 1024)]
        public async Task<IActionResult> Update([FromForm] UpdateLessonRequest request)
        {
            var userId = _currentUser.UserId;
            var response = await _lessonService.UpdateLessonAsync(request, userId!.Value);
            return this.ToActionResult(response);
        }
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteLessonRequest request)
        {
            var userId = _currentUser.UserId;
            var response = await _lessonService.DeleteLessonAsync(request, userId!.Value);
            return this.ToActionResult(response);
        }
    }
}
