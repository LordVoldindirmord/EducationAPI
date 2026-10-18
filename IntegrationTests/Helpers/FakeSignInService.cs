using Education.Domain.Entity;
using Education.Service.Auth.Interfaces;

namespace IntegrationTests.Helpers
{
    public sealed class FakeSignInService : ISignInService
    {
        public List<(Guid UserId, bool IsPersistent)> SignIns { get; } = [];

        public int SignOutCount { get; private set; }

        public Task SignInAsync(UserEntity user, bool isPersistent)
        {
            SignIns.Add((user.Id, isPersistent));
            return Task.CompletedTask;
        }

        public Task SignOutAsync()
        {
            SignOutCount++;
            return Task.CompletedTask;
        }
    }
}
