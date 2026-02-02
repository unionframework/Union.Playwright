using Union.Playwright.NUnit.Attributes;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
{
    public class QuestionsPage : StackOverflowPage
    {
        public override string AbsolutePath => "/questions";

        [UnionInit("div#questions")]
        public QuestionList Questions { get; set; }
    }
}
