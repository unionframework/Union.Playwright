using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Services
{
    public interface INavigationService
    {
        RequestData GetRequestData(IUnionPage page);
    }
}
