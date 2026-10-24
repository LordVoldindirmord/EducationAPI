using Education.Domain.Entity;
using Education.Domain.Enums;
using Education.Domain.Extensions;
using Education.Service.Common.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Education.Service.Common.Implementations
{
    public class PermissionService : IPermissionService
    {
        private readonly UserManager<UserEntity> _userManager;

        public PermissionService(UserManager<UserEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserRole?> GetUserRoleAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any())
                return null;

            var primaryRole = roles.FirstOrDefault();
            if (string.IsNullOrEmpty(primaryRole))
                return null;

            try
            {
                return primaryRole.DbStringToRole();
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        public async Task<bool> HasRoleAsync(Guid userId, UserRole role)
        {
            var userRole = await GetUserRoleAsync(userId);
            return userRole == role;
        }

        public async Task<bool> HasAnyRoleAsync(Guid userId, IEnumerable<UserRole> roles)
        {
            var role = await GetUserRoleAsync(userId);

            if (role == null)
                return false;

            return roles.Contains(role.Value);
        }

        public async Task<bool> IsActiveAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return false;

            if (await _userManager.IsLockedOutAsync(user))
                return false;

            return true;
        }
    }
}