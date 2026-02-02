using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Services
{
    public interface IWeb
    {
        public ServiceMatchResult MatchService(RequestData request);
        public RequestData GetRequestData(IUnionPage page);
        public void RegisterService(IUnionService service);
    }
}