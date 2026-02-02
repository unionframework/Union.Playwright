namespace Union.Playwright.NUnit.Routing
{
    public class BaseUrlInfo
    {
        public BaseUrlInfo(string domain, string absolutePath)
        {
            Domain = domain;
            AbsolutePath = absolutePath;
        }

        public string SubDomain { get; }

        public string Domain { get; }

        public string AbsolutePath { get; }

        public BaseUrlInfo ApplyActual(BaseUrlInfo baseUrlInfo)
        {
            var subDomain = baseUrlInfo == null || string.IsNullOrEmpty(baseUrlInfo.SubDomain)
                                ? SubDomain
                                : baseUrlInfo.SubDomain;
            var domain = baseUrlInfo == null || string.IsNullOrEmpty(baseUrlInfo.Domain)
                             ? Domain
                             : baseUrlInfo.Domain;
            var absolutePath = baseUrlInfo == null || string.IsNullOrEmpty(baseUrlInfo.AbsolutePath)
                                   ? AbsolutePath
                                   : baseUrlInfo.AbsolutePath;
            return new BaseUrlInfo(domain, absolutePath);
        }

        public string GetBaseUrl()
        {
            var s = Domain + AbsolutePath;
            if (!string.IsNullOrEmpty(SubDomain))
            {
                s = SubDomain + "." + s;
            }
            return s;
        }
    }

}