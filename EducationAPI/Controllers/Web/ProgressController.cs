using Education.Domain.DTO.Request.Courses.Progress;
using Education.Domain.Validation;
using Education.Service.Auth.Interfaces;
using Education.Service.Courses.Interfaces;
using EducationAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationAPI.Controllers.Web
{
    [Authorize]
    [Route("api/progress")]
    [ApiController]
    public class ProgressController : ControllerBase
    {
        private readonly IProgressService _progressService;
        private readonly ICurrentUserService _currentUser;

        public ProgressController(IProgressService progressService, ICurrentUserService currentUser)
        {
            _progressService = progressService;
            _currentUser = currentUser;
        }

        [HttpPost("complete/{lessonId:guid}")]
        public async Task<IActionResult> Complete([FromRoute, NotEmptyGuid] Guid lessonId)
        {
            var userId = _currentUser.UserId;
            var response = await _progressService.CompleteLessonAsync(userId!.Value, lessonId);
            return this.ToActionResult(response);
        }

        [HttpPost("submit-quiz")]
        public async Task<IActionResult> SubmitQuiz([FromBody] SubmitQuizRequest request)
        {
            var userId = _currentUser.UserId;
            var response = await _progressService.SubmitQuizAsync(userId!.Value, request);
            return this.ToActionResult(response);
        }
    }
}
