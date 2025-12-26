using System.Threading.Tasks;
using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Routing;

namespace Union.Playwright.Core
{
    public interface IBrowserGo
    {
        Task<T> ToPage<T>(bool inNewTab = false, int redirectTimeout = 0) where T : class, IUnionPage;
        Task ToPage(IUnionPage page, bool inNewTab = false, int redirectTimeout = 0);
        Task ToUrl(string url, bool inNewTab = false);
        Task ToUrl(RequestData requestData, bool inNewTab = false, int redirectTimeout = 0);
        Task Refresh();
        Task Back();
    }
}