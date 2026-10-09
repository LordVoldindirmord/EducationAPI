using Education.Service.Auth.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Education.Service.Auth.Implementations
{
    public class LinkGenerator : ILinkGenerator
    {
        private readonly IConfiguration _configuration;

        public LinkGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetConfirmEmailLink(Guid userId, string token)
        {
            return BuildLink("ConfirmEmailPath", userId, token);
        }

        public string GetResetPasswordLink(Guid userId, string token)
        {
            return BuildLink("ResetPasswordPath", userId, token);
        }

        private string BuildLink(string pathKey, Guid userId, string token)
        {
            var baseUrl = _configuration["API:BaseUrl"]
                ?? throw new InvalidOperationException("API:BaseUrl не настроен");

            var path = _configuration[$"API:{pathKey}"]
                ?? throw new InvalidOperationException($"API:{pathKey} не настроен");

            return $"{baseUrl}{path}?userId={userId}&token={Uri.EscapeDataString(token)}";
        }
    }
}
