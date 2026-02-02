using System.Threading.Tasks;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Core
{
    public interface IBrowserGo
    {
        Task<T> ToPage<T>() where T : class, IUnionPage;
        Task ToPage(IUnionPage page);
        Task ToUrl(string url);
        Task ToUrl(RequestData requestData);
        Task Refresh();
        Task Back();
    }
}
