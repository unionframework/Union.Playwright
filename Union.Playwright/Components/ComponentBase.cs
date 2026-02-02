using Microsoft.Playwright;
using System.Threading.Tasks;
using Union.Playwright.Pages.Interfaces;
using Union.Playwright.SCSS;

namespace Union.Playwright.Components
{
    public abstract class ComponentBase : IContainer
    {
        private string _rootScss;

        public IUnionPage ParentPage { get; }

        public string ComponentName { get; set; }

        public string FrameScss { get; set; }

        protected ComponentBase(IUnionPage parentPage, string rootScss = null)
        {
            this.ParentPage = parentPage;
            _rootScss = rootScss;
        }

        public virtual string RootScss => _rootScss ?? "html";

        protected IPage PlaywrightPage => this.ParentPage.PlaywrightPage;

        protected ILocator Root => this.PlaywrightPage.Locator(this.RootScss);

        public async Task<bool> IsVisibleAsync() => await this.Root.IsVisibleAsync();

        public string InnerScss(string relativeScss, params object[] args)
        {
            var formatted = string.Format(relativeScss, args);
            return ScssBuilder.Concat(this.RootScss, formatted).Value;
        }
    }
}
