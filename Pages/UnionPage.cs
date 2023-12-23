using Microsoft.Playwright;
using System.Collections.Generic;
using Union.Playwright.Pages.Interfaces;
using Union.Playwright.Routing;

namespace Union.Playwright.Pages
{
    public abstract class UnionPage : IUnionPage
    {
        private IPage _page;

        public List<ILoader> ProgressBars { get; private set; }

        public List<IModalWindow> Alerts { get; private set; }

        public BaseUrlInfo BaseUrlInfo { get; set; }

        public List<Cookie> Cookies { get; set; }

        public Dictionary<string, string> Params { get; set; }

        public Dictionary<string, string> Data { get; set; }

        public abstract string AbsolutePath { get; }

        public List<IModalWindow> ModalWindows { get; private set; }

        public List<ILoader> Loaders { get; private set; }

        public List<IOverlay> Overlays { get; private set; }

        protected UnionPage()
        {
            Params = new Dictionary<string, string>();
        }

        public void Activate(IPage page)
        {
            _page = page;
            WebPageBuilder.InitPage(this);
        }

        public virtual void WaitLoaded()
        {
        }

        public void RegisterComponent(IComponent component)
        {
            if (component is IModalWindow)
            {
                Alerts.Add(component as IModalWindow);
            }
            else if (component is ILoader)
            {
                ProgressBars.Add(component as ILoader);
            }
        }

        public T RegisterComponent<T>(string componentName, params object[] args) where T : IComponent
        {
            var component = CreateComponent<T>(args);
            RegisterComponent(component);
            component.ComponentName = componentName;
            return component;
        }

        public T CreateComponent<T>(params object[] args) where T : IComponent
        {
            return WebPageBuilder.CreateComponent<T>(this, args);
        }

        public RequestData GetRequest(BaseUrlInfo defaultBaseUrlInfo)
        {
            var url =
                new UriAssembler(BaseUrlInfo, AbsolutePath, Data, Params).Assemble(
                    defaultBaseUrlInfo);
            return new RequestData(url);
        }
    }
}
