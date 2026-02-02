using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Components
{
    public interface IContainer
    {
        IUnionPage ParentPage { get; }

        string RootScss { get; }

        string InnerScss(string relativeScss, params object[] args);
    }
}
