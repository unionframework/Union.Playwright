using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Services
{
    public interface IPageResolver
    {
        BaseUrlPattern BaseUrlPattern { get; }
        IUnionPage GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo);
    }
}
