using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Services
{
    public class BaseUrlMatchResult
    {
        public string AbsolutePath;

        public string Domain;

        public BaseUrlMatchLevel Level;

        public string SubDomain;

        public BaseUrlMatchResult(BaseUrlMatchLevel level, string subDomain, string domain, string absolutePath)
        {
            Level = level;
            SubDomain = subDomain;
            Domain = domain;
            AbsolutePath = absolutePath;
        }

        public static BaseUrlMatchResult Unmatched()
        {
            return new BaseUrlMatchResult(BaseUrlMatchLevel.Unmatched, null, null, null);
        }

        public BaseUrlInfo GetBaseUrlInfo()
        {
            return new BaseUrlInfo(Domain, AbsolutePath);
        }
    }



}