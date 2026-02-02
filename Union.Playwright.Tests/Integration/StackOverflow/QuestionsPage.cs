using Union.Playwright.Attributes;

namespace Union.Playwright.Tests.Integration.StackOverflow
{
    public class QuestionsPage : StackOverflowPage
    {
        public override string AbsolutePath => "/questions";

        [UnionInit("div#questions")]
        public QuestionList Questions { get; set; }
    }
}
