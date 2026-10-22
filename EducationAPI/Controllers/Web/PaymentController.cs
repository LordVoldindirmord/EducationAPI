using Education.Domain.DTO.Request.Payment;
using Education.Service.Auth.Interfaces;
using Education.Service.Purchases.Interfaces;
using EducationAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationAPI.Controllers.Web
{
    [Route("api/payment")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPurchaseService _purchaseService;
        private readonly ICurrentUserService _currentUser;

        public PaymentController(
            IPurchaseService purchaseService,
            ICurrentUserService currentUser)
        {
            _purchaseService = purchaseService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Инициировать покупку курса
        /// </summary>
        [HttpPost("course")]
        public async Task<IActionResult> BuyCourse([FromBody] PurchaseRequest request)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Unauthorized();

            var response = await _purchaseService.InitiateCoursePurchaseAsync(userId.Value, request.CourseId, request.ReturnUrl);

            return this.ToActionResult(response);
        }

        /// <summary>
        /// Инициировать покупку роли
        /// </summary>
        [HttpPost("role")]
        public async Task<IActionResult> BuyRole([FromBody] RolePurchaseRequest request)
        {
            var userId = _currentUser.UserId;

            if (userId == null)
                return Unauthorized();

            var response = await _purchaseService.InitiateRolePurchaseAsync(userId.Value, request.RoleName, request.ReturnUrl);

            return this.ToActionResult(response);
        }
    }
}
