using Union.Playwright.Core;
using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Routing;

namespace Union.Playwright.Services
{
    public interface IUnionService
    {
        string BaseUrl { get; }

        public BaseUrlPattern BaseUrlPattern { get; }

        bool HasPage(IUnionPage page);

        IUnionPage GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo);

        RequestData GetRequestData(IUnionPage page);

        IServiceContextsPool ServiceContextsPool { get; }

        IBrowserState State { get; }

        IBrowserGo Go { get; }
    }
}