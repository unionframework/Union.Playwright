using Union.Playwright.Routing;

namespace Union.Playwright.Services
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