using Education.Domain.DTO.Request.Courses.Modules;
using Education.Service.Auth.Interfaces;
using Education.Service.Courses.Interfaces;
using EducationAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationAPI.Controllers.Web
{
    [Authorize]
    [Route("api/module")]
    [ApiController]
    public class ModuleController : ControllerBase
    {
        private readonly IModuleService _moduleService;
        private readonly ICurrentUserService _currentUser;

        public ModuleController(IModuleService moduleService, ICurrentUserService currentUser)
        {
            _moduleService = moduleService;
            _currentUser = currentUser;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateModuleRequest request)
        {
            var userId = _currentUser.UserId;
            var response = await _moduleService.CreateModuleAsync(request, userId!.Value);
            return this.ToActionResult(response);
        }
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateModuleRequest request)
        {
            var userId = _currentUser.UserId;
            var response = await _moduleService.UpdateModuleAsync(request, userId!.Value);
            return this.ToActionResult(response);
        }
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteModuleRequest request)
        {
            var userId = _currentUser.UserId;
            var response = await _moduleService.DeleteModuleAsync(request, userId!.Value);
            return this.ToActionResult(response);
        }
    }
}
