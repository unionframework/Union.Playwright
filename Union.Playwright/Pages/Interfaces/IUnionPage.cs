using Microsoft.Playwright;
using System.Collections.Generic;
using Union.Playwright.Routing;

namespace Union.Playwright.Pages.Interfaces
{
    public interface IUnionPage
    {
        string AbsolutePath { get; }

        List<Cookie> Cookies { get; set; }

        Dictionary<string, string> Data { get; set; }

        Dictionary<string, string> Params { get; set; }

        BaseUrlInfo BaseUrlInfo { get; set; }

        List<IModalWindow> ModalWindows { get; }

        List<ILoader> Loaders { get; }

        List<IOverlay> Overlays { get; }

        //void RegisterComponent(IComponent component);

        //T RegisterComponent<T>(string componentName, params object[] args) where T : IComponent;

        //T CreateComponent<T>(params object[] args) where T : IComponent;
        //void Activate(Browser.Browser browser, IUnionLogger log, string windowHandle);

        void WaitLoaded();

        RequestData GetRequest(BaseUrlInfo defaultBaseUrlInfo);
        public void Activate(IPage page);
    }
}
