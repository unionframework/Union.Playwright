using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
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
