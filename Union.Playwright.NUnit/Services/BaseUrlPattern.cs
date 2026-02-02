using System.Text.RegularExpressions;

namespace Union.Playwright.NUnit.Services
{
    public class BaseUrlPattern
    {
        private readonly string _regexPattern;
        private readonly Regex _regex;

        public BaseUrlPattern(string regexPattern)
        {
            _regexPattern = regexPattern;
            _regex = new Regex(regexPattern, RegexOptions.Compiled);
        }

        public int Length => _regexPattern.Length;

        public BaseUrlMatchResult Match(string url)
        {
            var match = _regex.Match(url);
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