using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Routing;

namespace Union.Playwright.Services
{
    public interface IPageResolver
    {
        BaseUrlPattern BaseUrlPattern { get; }
        IUnionPage GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo);
    }
}
