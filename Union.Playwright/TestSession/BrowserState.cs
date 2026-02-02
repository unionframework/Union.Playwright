using Microsoft.Playwright;
using Union.Playwright.Core;
using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Services;

namespace Union.Playwright.TestSession
{
    internal class BrowserState : IBrowserState
    {
        private readonly IPageResolver _pageResolver;
        public IModalWindow? ModalWindow {  get; private set; }
        public IUnionPage? Page {  get; private set; }

        public BrowserState(IPageResolver pageResolver)
        {
            _pageResolver = pageResolver;
        }

        public void Actualize(IPage page)
        {
            this.Page = null;
            var baseUrlPattern = _pageResolver.BaseUrlPattern;
            var result = baseUrlPattern.Match(page.Url);
            if (result.Level == BaseUrlMatchLevel.FullDomain)
            {
                this.Page = _pageResolver.GetPage(new Routing.RequestData(page.Url), result.GetBaseUrlInfo());
                this.Page.Activate(page);
            }
        }

        public T? PageAs<T>() where T : class, IUnionPage => this.Page as T;

        public bool PageIs<T>() where T : IUnionPage
        {
            if (this.Page == null)
            {
                return false;
            }

            return this.Page is T;
        }
    }
}
