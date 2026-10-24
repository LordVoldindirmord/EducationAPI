using Education.Domain.Entity;
using Education.Service.Auth.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Education.Service.Auth.Implementations
{
    public class SignInService : ISignInService
    {
        private readonly SignInManager<UserEntity> _signInManager;

        public SignInService(SignInManager<UserEntity> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task SignInAsync(UserEntity user, bool isPersistent)
        {
            await _signInManager.SignInAsync(user, isPersistent);
        }

        public async Task SignOutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}