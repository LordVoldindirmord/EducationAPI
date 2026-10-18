using System.Text.RegularExpressions;
using FluentAssertions;

namespace IntegrationTests.Helpers
{
    public static class EmailLinkParser
    {
        public static string ExtractQueryParam(string html, string name)
        {
            var match = Regex.Match(html, $"{Regex.Escape(name)}=([^\"'&\\s<]+)");
            match.Success.Should().BeTrue($"в письме должен быть параметр {name}");
            return Uri.UnescapeDataString(match.Groups[1].Value);
        }
    }
}
