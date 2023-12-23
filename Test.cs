using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace PlaywrightTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class Tests : PageTest
{
    Tests()
    {
    }

    [Test]
    public async Task HomepageHasPlaywrightInTitleAndGetStartedLinkLinkingtoTheIntroPage()
    {
        //await myservice.GotoMyPage()
        //myservice.State.PageAs<MyPage>()
        //TestSessionProvider.Instance.GetService<MyService>(this);
        //TestSessionProvider.Instance.GetTestSession(this);

        //myservice.Go.ToPage<AnotherServicePage>()
        //myservice.Context
        //myservice.State.ActualizePage()
        //myservice.State.ActualizePopup(0)
        //myservice.State.Page
        //myservice.State.PageAs<FirstPage>()
        //myservice.State.Popups[0]
        //myservice.State.PopupAs<SecondPage>(0)
        //myservice.State.Page.ClickAndWaitForRedirectAsync<MyFirstPage>()
        //myservice.State.Page.ClickAndWaitForModalAsync<MyModal>()
        //myservice.State.Page.ClickAndWaitForPopupAsync<MySecondPage>()
        //myservice.State.Page.WaitLoadedAsync()
        //myservice.State.Page.WaitWhileLoadingAsync()
        //myservice.State.Page.WaitForModalAsync<MyModal>()

        await Page.GotoAsync("https://playwright.dev");

        // Expect a title "to contain" a substring.
        await Expect(Page).ToHaveTitleAsync(new Regex("Playwright"));

        // create a locator
        var getStarted = Page.GetByRole(AriaRole.Link, new() { Name = "Get started" });

        // Expect an attribute "to be strictly equal" to the value.
        await Expect(getStarted).ToHaveAttributeAsync("href", "/docs/intro");

        // Click the get started link.
        await getStarted.ClickAsync();

        // Expects the URL to contain intro.
        await Expect(Page).ToHaveURLAsync(new Regex(".*intro"));
    }
}