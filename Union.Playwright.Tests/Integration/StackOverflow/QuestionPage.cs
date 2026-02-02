namespace Union.Playwright.Tests.Integration.StackOverflow
{
    public class QuestionPage : StackOverflowPage
    {
        public override string AbsolutePath => "/questions/{questionId}/{slug}";
    }
}
