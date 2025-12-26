using System.Collections.Generic;
using System;
using Union.Playwright.Routing;
using System.Linq;
using Union.Playwright.TestSession;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Services
{
    public class Web : IWeb
    {
        private readonly List<IUnionService> _services;

        public Web()
        {
            _services = new List<IUnionService>();
        }

        public ServiceMatchResult MatchService(RequestData request)
        {
            ServiceMatchResult baseDomainMatch = null;
            foreach (var service in _services)
            {
                var baseUrlPattern = service.BaseUrlPattern;
                var result = baseUrlPattern.Match(request.Url.OriginalString);
                if (result.Level == BaseUrlMatchLevel.FullDomain)
                {
                    return new ServiceMatchResult(service, result.GetBaseUrlInfo());
                }
                if (result.Level == BaseUrlMatchLevel.BaseDomain)
                {
                    if (baseDomainMatch != null)
                    {
                        throw new Exception(string.Format("Two BaseDomain matches for url {0}", request.Url));
                    }
                    baseDomainMatch = new ServiceMatchResult(service, result.GetBaseUrlInfo());
                }
            }
            return baseDomainMatch;
        }

        public RequestData GetRequestData(IUnionPage page)
        {
            var service = _services.FirstOrDefault(s => s.HasPage(page));
            if (service == null)
            {
                throw new PageNotRegisteredException(page);
            }
            return service.GetRequestData(page);
        }

        public void RegisterService(IUnionService service)
        {
            _services.Add(service);
            _services.Sort((s1, s2) =>
            {
                if (s1.BaseUrl.Length == s2.BaseUrl.Length)
                    return 0;
                return s1.BaseUrl.Length > s2.BaseUrl.Length ? -1 : 1;
            });
        }
    }
}