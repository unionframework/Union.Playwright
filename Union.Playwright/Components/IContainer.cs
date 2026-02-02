using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Components
{
    public interface IContainer
    {
        IUnionPage ParentPage { get; }

        string RootScss { get; }

        string InnerScss(string relativeScss, params object[] args);
    }
}
