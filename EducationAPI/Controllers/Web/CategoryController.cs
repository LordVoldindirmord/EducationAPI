using Education.Domain.DTO.Request.Courses.Categories;
using Education.Service.Auth.Interfaces;
using Education.Service.Courses.Interfaces;
using EducationAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationAPI.Controllers.Web
{
    [Route("api/category")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ICurrentUserService _currentUser;

        public CategoryController(
            ICategoryService categoryService,
            ICurrentUserService currentUser)
        {
            _categoryService = categoryService;
            _currentUser = currentUser;
        }

        [Authorize(Policy = AuthorizationExtension.ADMIN_OR_MODERATOR)]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Unauthorized();

            var response = await _categoryService.CreateCategoryAsync(request, userId.Value);

            return this.ToActionResult(response);
        }

        [Authorize(Policy = AuthorizationExtension.ADMIN_OR_MODERATOR)]
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteCategoryRequest request)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Unauthorized();

            var response = await _categoryService.DeleteCategoryAsync(request, userId.Value);

            return this.ToActionResult(response);
        }

        [Authorize (Policy = AuthorizationExtension.ADMIN_OR_MODERATOR)]
        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateCategoryRequest request)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Unauthorized();

            var response = await _categoryService.UpdateCategoryAsync(request, userId.Value);

            return this.ToActionResult(response);
        }
    }
}
