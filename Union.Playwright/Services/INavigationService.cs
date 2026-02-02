using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Routing;

namespace Union.Playwright.Services
{
    public interface INavigationService
    {
        RequestData GetRequestData(IUnionPage page);
    }
}
