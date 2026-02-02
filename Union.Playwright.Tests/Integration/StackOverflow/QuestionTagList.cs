using Union.Playwright.Attributes;
using Union.Playwright.Components;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Tests.Integration.StackOverflow
{
    public class QuestionTagList : ComponentBase
    {
        [UnionInit("a.s-tag")]
        public Element TagLink { get; set; }

        public QuestionTagList(IUnionPage parentPage, string rootScss)
            : base(parentPage, rootScss)
        {
        }

        public async Task<List<string>> GetTagNamesAsync()
        {
            var links = this.PlaywrightPage.Locator(this.InnerScss("a.s-tag"));
            var count = await links.CountAsync();
            var names = new List<string>();
            for (int i = 0; i < count; i++)
            {
                names.Add(await links.Nth(i).TextContentAsync());
            }

            return names;
        }
    }
}
