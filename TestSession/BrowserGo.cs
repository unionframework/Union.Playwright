using Union.Playwright.Core;
using System.Threading.Tasks;
using Union.Playwright.Routing;
using System;
using Microsoft.Extensions.Logging;
using Union.Playwright.Services;
using Microsoft.Playwright;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.TestSession
{
    public class BrowserGo:IBrowserGo
    {
        private IServiceContextsPool _serviceContextsPool;
        private IUnionService _service;
        private IBrowserState _state;
        private ILogger _logger;

        public BrowserGo(IUnionService service, IBrowserState state, IServiceContextsPool serviceContextsPool)
        {
            _service = service;
            _state = state;
            _serviceContextsPool = serviceContextsPool;
            //_logger = logger;
        }

        private async Task<IPage> GetPageAsync()
        {
            var context = await _serviceContextsPool.GetContext(_service).ConfigureAwait(false);
            if (context.Pages.Count == 0)
            {
                await context.NewPageAsync().ConfigureAwait(false);
            }
            return context.Pages[0];
        }

        public async virtual Task<T> ToPage<T>(bool inNewTab = false, int redirectTimeout = 0) where T : class, IUnionPage
        {
            var pageInstance = (T)Activator.CreateInstance(typeof(T));
            await ToPage(pageInstance, inNewTab, redirectTimeout);
            return _state.PageAs<T>();
        }

        public async Task ToPage(IUnionPage page, bool inNewTab = false, int redirectTimeout = 0)
        {
            var requestData = _service.GetRequestData(page);
            await ToUrl(requestData, inNewTab, redirectTimeout);
        }

        public async Task ToUrl(string url, bool inNewTab = false)
        {
            await ToUrl(new RequestData(url), inNewTab);
        }

        public async Task ToUrl(RequestData requestData, bool inNewTab = false, int redirectTimeout = 0)
        {
            var page = await GetPageAsync();
            await page.GotoAsync(requestData.Url.ToString());
            AfterNavigate(page);
        }

        public async Task Refresh()
        {
            var page = await GetPageAsync();
            await page.ReloadAsync();
            AfterNavigate(page);
        }

        public async Task Back()
        {
            var page = await GetPageAsync();
            await page.GoBackAsync();
            AfterNavigate(page);
        }

        private void AfterNavigate(IPage page)
        {
            _state.Actualize(page);
            if (_state.PageIs<IUnionPage>())
            {
                _state.PageAs<IUnionPage>().WaitLoaded();
            }
        }
    }
}
