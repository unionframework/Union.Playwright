using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
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
