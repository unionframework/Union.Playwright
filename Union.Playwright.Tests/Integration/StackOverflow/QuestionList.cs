using Union.Playwright.Components;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Tests.Integration.StackOverflow
{
    public class QuestionList : ListBase<QuestionItem>
    {
        public override string ItemIdScss => "div.s-post-summary";

        public override string IdAttribute => "data-post-id";

        public QuestionList(IUnionPage parentPage, string rootScss)
            : base(parentPage, rootScss)
        {
        }
    }
}
