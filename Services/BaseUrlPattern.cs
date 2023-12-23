using System.Text.RegularExpressions;

namespace Union.Playwright.Services
{
    public class BaseUrlPattern
    {
        private readonly string _regexPattern;

        public BaseUrlPattern(string regexPattern)
        {
            _regexPattern = regexPattern;
        }

        public int Length => _regexPattern.Length;

        public BaseUrlMatchResult Match(string url)
        {
            var regex = new Regex(_regexPattern);
            var match = regex.Match(url);
            if (!match.Success)
            {
                return BaseUrlMatchResult.Unmatched();
            }
            var domain = match.Groups["domain"].Value;
            var abspath = HasGroup(match, "abspath") ? match.Groups["abspath"].Value : "";

            if (HasGroup(match, "subdomain"))
            {
                return new BaseUrlMatchResult(
                    BaseUrlMatchLevel.FullDomain,
                    match.Groups["subdomain"].Value,
                    domain,
                    abspath);
            }

            var optionalsubdomain = match.Groups["optionalsubdomain"].Value;
            if (string.IsNullOrEmpty(optionalsubdomain))
            {
                return new BaseUrlMatchResult(BaseUrlMatchLevel.FullDomain, null, domain, abspath);
            }

            return new BaseUrlMatchResult(BaseUrlMatchLevel.BaseDomain, optionalsubdomain, domain, abspath);
        }

        private bool HasGroup(Match match, string groupName)
        {
            return !string.IsNullOrEmpty(match.Groups[groupName].Value);
        }
    }



}