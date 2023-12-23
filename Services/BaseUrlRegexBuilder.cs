using System.Collections.Generic;
using System.Linq;

namespace Union.Playwright.Services
{
    public class BaseUrlRegexBuilder
    {
        private readonly string _domainPattern;

        private string _absolutePathPattern = "";

        private string _subDomainPattern = "((?<optionalsubdomain>[^\\.]+)\\.)?";

        public BaseUrlRegexBuilder(string domain)
            : this(new List<string> { domain })
        {
        }

        public BaseUrlRegexBuilder(List<string> domains)
        {
            _domainPattern = GenerateDomainsPattern(domains);
        }

        private string GenerateDomainsPattern(List<string> domains)
        {
            var s = domains.Aggregate("", (current, domain) => current + domain + "|");
            s = s.Substring(0, s.Length - 1);
            s = s.Replace(".", "\\.");
            return string.Format("(?<domain>({0}))", s);
        }

        public void SetSubDomain(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _subDomainPattern = $"(?<subdomain>{value})\\.";
            }
        }

        public void SetAbsolutePathPattern(string pattern)
        {
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                _absolutePathPattern = $"(?<abspath>{pattern})";
            }
        }

        public string Build()
        {
            return "^(http(|s):\\/\\/|)(www.|)" + _subDomainPattern + _domainPattern + _absolutePathPattern
                   + ".*";
        }
    }


}