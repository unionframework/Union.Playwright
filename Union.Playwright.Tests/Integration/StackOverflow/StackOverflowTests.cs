using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Union.Playwright.Core;
using Union.Playwright.Pages;
using Union.Playwright.TestSession;

namespace Union.Playwright.Tests.Integration.StackOverflow
{
    [TestFixture]
    public class StackOverflowTests : UnionTest<StackOverflowTestSession>
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<StackOverflowService>();
        }

        [SetUp]
        public async Task SetUpRoute()
        {
            // Load the HTML fixture file (copied to output directory)
            var htmlPath = Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                "Integration", "StackOverflow", "questions.html");
            var htmlContent = await File.ReadAllTextAsync(htmlPath);

            // Intercept requests to stackoverflow.com and serve local HTML
            await this.Page.RouteAsync("**/questions", async route =>
            {
                await route.FulfillAsync(new()
                {
                    ContentType = "text/html",
                    Body = htmlContent
                });
            });
        }

        [Test]
        public async Task NavigateToQuestions_PageIsQuestionsPage()
        {
            var page = await this.Session.SO.Go.ToPage<QuestionsPage>();

            this.Session.SO.State.PageIs<QuestionsPage>().Should().BeTrue();
            page.Should().NotBeNull();
        }

        [Test]
        public async Task QuestionsPage_GetIds_ReturnsNonEmpty()
        {
            var page = await this.Session.SO.Go.ToPage<QuestionsPage>();
            page.Should().NotBeNull("ToPage should return a QuestionsPage");
            page!.Questions.Should().NotBeNull("Questions should be initialized by WebPageBuilder");

            var ids = await page.Questions.GetIdsAsync();

            ids.Should().NotBeEmpty();
            ids.Should().HaveCount(3);
            ids.Should().Contain("79880001");
            ids.Should().Contain("79880002");
            ids.Should().Contain("79880003");
        }

        [Test]
        public async Task QuestionsPage_GetItems_TitleIsInitializedAndVisible()
        {
            var page = await this.Session.SO.Go.ToPage<QuestionsPage>();
            var items = await page!.Questions.GetItemsAsync();
            items.Should().HaveCount(3);

            var firstItem = items[0];
            WebPageBuilder.InitComponent(page, firstItem);

            firstItem.Title.Should().NotBeNull();
            var isVisible = await firstItem.Title.IsVisibleAsync();
            isVisible.Should().BeTrue();
        }

        [Test]
        public async Task QuestionsPage_FirstItem_HasTags()
        {
            var page = await this.Session.SO.Go.ToPage<QuestionsPage>();
            var firstItem = await page!.Questions.FindSingleAsync();
            firstItem.Should().NotBeNull();

            WebPageBuilder.InitComponent(page, firstItem!);

            var tagNames = await firstItem!.Tags.GetTagNamesAsync();
            tagNames.Should().NotBeEmpty();
            tagNames.Should().Contain("playwright");
            tagNames.Should().Contain("testing");
            tagNames.Should().Contain("csharp");
        }

        [Test]
        public async Task QuestionsPage_FirstItem_TagLinkIsVisible()
        {
            var page = await this.Session.SO.Go.ToPage<QuestionsPage>();
            var firstItem = await page!.Questions.FindSingleAsync();
            firstItem.Should().NotBeNull();

            WebPageBuilder.InitComponent(page, firstItem!);

            firstItem!.Tags.Should().NotBeNull();
            firstItem.Tags.TagLink.Should().NotBeNull();
            // TagLink selector matches multiple a.s-tag elements; verify the first is visible
            var tagLinkLocator = this.Page.Locator(firstItem.Tags.TagLink.RootScss).First;
            var isVisible = await tagLinkLocator.IsVisibleAsync();
            isVisible.Should().BeTrue();
        }
    }
}
