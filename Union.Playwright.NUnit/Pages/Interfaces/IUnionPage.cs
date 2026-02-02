using Microsoft.Playwright;
using System.Collections.Generic;
using System.Threading.Tasks;
using Union.Playwright.NUnit.Routing;

namespace Union.Playwright.NUnit.Pages.Interfaces
{
    public interface IUnionPage
    {
        string AbsolutePath { get; }

        IPage PlaywrightPage { get; }

        List<Cookie> Cookies { get; set; }

        Dictionary<string, string> Data { get; set; }

        Dictionary<string, string> Params { get; set; }

        BaseUrlInfo BaseUrlInfo { get; set; }

        List<IModalWindow> ModalWindows { get; }

        List<ILoader> Loaders { get; }

        List<IOverlay> Overlays { get; }

        Task WaitLoadedAsync();

        RequestData GetRequest(BaseUrlInfo defaultBaseUrlInfo);

        void Activate(IPage page);
    }
}
