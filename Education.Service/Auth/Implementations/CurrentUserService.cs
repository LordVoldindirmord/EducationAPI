using Education.Domain.Enums;
using Education.Domain.Extensions;
using Education.Service.Auth.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Education.Service.Auth.Implementations
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public Guid? UserId
        {
            get
            {
                var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);

                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

        public string? UserName => User?.FindFirstValue(ClaimTypes.Name);

        public string? Email => User?.FindFirstValue(ClaimTypes.Email);

        public UserRole? Role
        {
            get
            {
                var value = User?.FindFirstValue(ClaimTypes.Role);

                if (string.IsNullOrEmpty(value))
                    return null;

                try
                {
                    return value.DbStringToRole();
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
            }
        }

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    }
}