using System;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;
using Union.Playwright.NUnit.TestSession;

namespace Union.Playwright.NUnit.Services
{
    public abstract class UnionService<T> : IUnionService where T : IUnionPage
    {
        private IRouter _router;
        private readonly IServiceContextsPool _serviceContextsPool;
        public IServiceContextsPool ServiceContextsPool => _serviceContextsPool;

        private IBrowserState _state;
        public IBrowserState State => _state ??= new BrowserState(this);

        private IBrowserGo _go;
        public IBrowserGo Go => _go ??= new BrowserGo(this, State, _serviceContextsPool);

        public UnionService(IServiceContextsPool serviceContextsPool)
        {
            _serviceContextsPool = serviceContextsPool;
            var matchUrlRouter = new MatchUrlRouter();
            matchUrlRouter.RegisterDerivedPages<T>();
            _router = matchUrlRouter;
        }

        public abstract string BaseUrl { get; }

        private Uri BaseUri => new Uri(BaseUrl);

        public string AbsolutePath => BaseUri.AbsolutePath == "/" ? "" : BaseUri.AbsolutePath;

        public string Host => BaseUri.Authority;

        private BaseUrlPattern _baseUrlPattern;
        public BaseUrlPattern BaseUrlPattern => _baseUrlPattern ??= BuildBaseUrlPattern();

        private BaseUrlPattern BuildBaseUrlPattern()
        {
            var urlRegexBuilder = new BaseUrlRegexBuilder(Host);
            if (!string.IsNullOrWhiteSpace(AbsolutePath))
            {
                urlRegexBuilder.SetAbsolutePathPattern(AbsolutePath.Replace("/", "\\/"));
            }
            return new BaseUrlPattern(urlRegexBuilder.Build());
        }

        private BaseUrlInfo DefaultBaseUrlInfo => new BaseUrlInfo(Host, AbsolutePath);

        public IUnionPage GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo)
        {
            return _router.GetPage(requestData, baseUrlInfo);
        }

        public RequestData GetRequestData(IUnionPage page)
        {
            return _router.GetRequest(page, DefaultBaseUrlInfo);
        }

        public bool HasPage(IUnionPage page)
        {
            return _router.HasPage(page);
        }
    }


}