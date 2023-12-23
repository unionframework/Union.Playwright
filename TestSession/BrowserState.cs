using Microsoft.Playwright;
using Union.Playwright.Core;
using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Services;

namespace Union.Playwright.TestSession
{
    internal class BrowserState : IBrowserState
    {
        private IUnionService _service;
        public IModalWindow? ModalWindow {  get; private set; }
        public IUnionPage? Page {  get; private set; }

        public BrowserState(IUnionService service)
        {
            _service = service;
        }

        public void Actualize(IPage page)
        {
            Page = null;
            var baseUrlPattern = _service.BaseUrlPattern;
            var result = baseUrlPattern.Match(page.Url);
            if (result.Level == BaseUrlMatchLevel.FullDomain)
            {
                new ServiceMatchResult(_service, result.GetBaseUrlInfo());
                Page = _service.GetPage(new Routing.RequestData(page.Url), result.GetBaseUrlInfo());
                Page.Activate(page);
            }
        }

        public T? PageAs<T>() where T : class, IUnionPage => Page as T;

        public bool PageIs<T>() where T : IUnionPage
        {
            if (Page == null)
            {
                return false;
            }

            return Page is T;
        }
    }
}