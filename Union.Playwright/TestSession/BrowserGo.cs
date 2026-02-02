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
    public class BrowserGo : IBrowserGo
    {
        private readonly IServiceContextsPool _serviceContextsPool;
        private readonly IUnionService _service;
        private readonly INavigationService _navigationService;
        private readonly IBrowserState _state;
        private ILogger _logger;

        public BrowserGo(IUnionService service, IBrowserState state, IServiceContextsPool serviceContextsPool)
        {
            _service = service;
            _navigationService = service;
            _state = state;
            _serviceContextsPool = serviceContextsPool;
        }

        private async Task<IPage> GetPageAsync()
        {
            var context = await _serviceContextsPool.GetContext(_service).ConfigureAwait(false);
            if (context.Pages.Count == 0)
            {
                await context.NewPageAsync().ConfigureAwait(false);
            }
            // TODO: get active page instead of first
            return context.Pages[0];
        }

        public async virtual Task<T> ToPage<T>() where T : class, IUnionPage
        {
            var pageInstance = (T)Activator.CreateInstance(typeof(T));
            await this.ToPage(pageInstance);
            return _state.PageAs<T>()
                ?? throw new InvalidOperationException(
                    $"Navigation did not resolve to {typeof(T).Name}. Current URL may not match the expected page pattern.");
        }

        public async Task ToPage(IUnionPage page)
        {
            var requestData = _navigationService.GetRequestData(page);
            await this.ToUrl(requestData);
        }

        public async Task ToUrl(string url)
        {
            await this.ToUrl(new RequestData(url));
        }

        public async Task ToUrl(RequestData requestData)
        {
            var page = await this.GetPageAsync();
            await page.GotoAsync(requestData.Url.ToString());
            await this.AfterNavigateAsync(page);
        }

        public async Task Refresh()
        {
            var page = await this.GetPageAsync();
            await page.ReloadAsync();
            await this.AfterNavigateAsync(page);
        }

        public async Task Back()
        {
            var page = await this.GetPageAsync();
            await page.GoBackAsync();
            await this.AfterNavigateAsync(page);
        }

        private async Task AfterNavigateAsync(IPage page)
        {
            _state.Actualize(page);
            if (_state.PageIs<IUnionPage>())
            {
                await _state.PageAs<IUnionPage>().WaitLoadedAsync();
            }
        }
    }
}
