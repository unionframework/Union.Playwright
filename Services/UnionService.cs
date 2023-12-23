using System;
using Union.Playwright.Core;
using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Routing;
using Union.Playwright.TestSession;

namespace Union.Playwright.Services
{
    public abstract class UnionService<T> : IUnionService where T : IUnionPage
    {
        private IRouter _router;
        private readonly IServiceContextsPool _serviceContextsPool;
        public IServiceContextsPool ServiceContextsPool => _serviceContextsPool;

        private IBrowserState _state;
        public IBrowserState State => _state??(_state=new BrowserState(this));

        private IBrowserGo _go;
        public IBrowserGo Go => _go??(_go=new BrowserGo(this, State, _serviceContextsPool));

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

        public BaseUrlPattern BaseUrlPattern
        {
            get
            {
                var urlRegexBuilder = new BaseUrlRegexBuilder(Host);
                if (!string.IsNullOrWhiteSpace(AbsolutePath))
                {
                    urlRegexBuilder.SetAbsolutePathPattern(AbsolutePath.Replace("/", "\\/"));
                }
                return new BaseUrlPattern(urlRegexBuilder.Build());
            }
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