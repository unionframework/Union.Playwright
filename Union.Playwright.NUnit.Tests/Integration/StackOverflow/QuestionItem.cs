using Union.Playwright.NUnit.Attributes;
using Union.Playwright.NUnit.Components;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
{
    public class QuestionItem : ItemBase
    {
        public override string ItemScss => $"div.s-post-summary[data-post-id='{this.Id}']";

        [UnionInit("h3 a")]
        public Element Title { get; set; }

        [UnionInit("ul.ml0")]
        public QuestionTagList Tags { get; set; }

        public QuestionItem(IContainer container, string id)
            : base(container, id)
        {
        }
    }
}
