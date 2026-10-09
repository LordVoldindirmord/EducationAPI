using Education.Domain.DTO.Request.Authentication;
using Education.Service.Auth.Interfaces;
using EducationAPI.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EducationAPI.Controllers.Web
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var response = await _authService.RegisterAsync(request);

            return this.ToActionResult(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);

            return this.ToActionResult(response);
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            var response = await _authService.ConfirmEmailAsync(request);

            return this.ToActionResult(response);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var response = await _authService.ForgotPasswordAsync(request);

            return this.ToActionResult(response);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var response = await _authService.ResetPasswordAsync(request);

            return this.ToActionResult(response);
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
        {
            var response = await _authService.ResendConfirmationAsync(request);

            return this.ToActionResult(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var response = await _authService.LogoutAsync();

            return this.ToActionResult(response);
        }
    }
}
