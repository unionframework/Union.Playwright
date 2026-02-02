using Union.Playwright.Components;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Tests.Integration.StackOverflow
{
    /// <summary>
    /// Simple concrete ComponentBase for use with [UnionInit] on generic elements.
    /// </summary>
    public class Element : ComponentBase
    {
        public Element(IUnionPage parentPage, string rootScss)
            : base(parentPage, rootScss)
        {
        }
    }
}
