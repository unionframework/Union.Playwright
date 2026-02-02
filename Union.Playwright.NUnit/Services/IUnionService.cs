using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Services
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