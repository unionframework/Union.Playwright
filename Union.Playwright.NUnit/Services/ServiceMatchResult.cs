using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Services
{
    public class ServiceMatchResult
    {
        private readonly BaseUrlInfo _baseUrlInfo;

        private readonly IUnionService _service;

        public ServiceMatchResult(IUnionService service, BaseUrlInfo baseUrlInfo)
        {
            _service = service;
            _baseUrlInfo = baseUrlInfo;
        }

        public IUnionService GetService()
        {
            return _service;
        }

        public BaseUrlInfo GetBaseUrlInfo()
        {
            return _baseUrlInfo;
        }
    }



}