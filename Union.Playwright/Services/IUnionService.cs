using Union.Playwright.Core;
using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Routing;

namespace Union.Playwright.Services
{
    public interface IUnionService : IPageResolver, INavigationService
    {
        string BaseUrl { get; }

        bool HasPage(IUnionPage page);

        IServiceContextsPool ServiceContextsPool { get; }

        IBrowserState State { get; }

        IBrowserGo Go { get; }
    }
}