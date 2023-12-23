using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Routing;

namespace Union.Playwright.Services
{
    public interface IWeb
    {
        public ServiceMatchResult MatchService(RequestData request);
        public RequestData GetRequestData(IUnionPage page);
        public void RegisterService(IUnionService service);
    }
}