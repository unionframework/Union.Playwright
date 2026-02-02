using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Union.Playwright.Components;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Tests.Components
{
    public class TestComponent : ComponentBase
    {
        public TestComponent(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }
    }

    [TestFixture]
    public class ComponentBaseTests
    {
        private IUnionPage _mockPage;
        private IPage _mockPlaywrightPage;

        [SetUp]
        public void SetUp()
        {
            _mockPage = Substitute.For<IUnionPage>();
            _mockPlaywrightPage = Substitute.For<IPage>();
            _mockPage.PlaywrightPage.Returns(_mockPlaywrightPage);
        }

        [Test]
        public void ParentPage_ReturnsProvidedPage()
        {
            var component = new TestComponent(_mockPage);

            component.ParentPage.Should().BeSameAs(_mockPage);
        }

        [Test]
        public void RootScss_WhenNoScssProvided_DefaultsToHtml()
        {
            var component = new TestComponent(_mockPage);

            component.RootScss.Should().Be("html");
        }

        [Test]
        public void RootScss_WhenScssProvided_ReturnsIt()
        {
            var component = new TestComponent(_mockPage, ".my-component");

            component.RootScss.Should().Be(".my-component");
        }

        [Test]
        public void InnerScss_ConcatenatesRootAndRelative()
        {
            var component = new TestComponent(_mockPage, "div.container");

            var result = component.InnerScss("span.child");

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void InnerScss_FormatsArgsIntoRelativeScss()
        {
            var component = new TestComponent(_mockPage, "div.container");

            var result = component.InnerScss("span[data-id='{0}']", "test-id");

            result.Should().Contain("test-id");
        }

        [Test]
        public void ComponentName_CanBeSetAndRetrieved()
        {
            var component = new TestComponent(_mockPage);
            component.ComponentName = "My Component";

            component.ComponentName.Should().Be("My Component");
        }

        [Test]
        public void FrameScss_CanBeSetAndRetrieved()
        {
            var component = new TestComponent(_mockPage);
            component.FrameScss = "iframe.main";

            component.FrameScss.Should().Be("iframe.main");
        }

        [Test]
        public async Task IsVisibleAsync_DelegatesToLocator()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.IsVisibleAsync(Arg.Any<LocatorIsVisibleOptions>()).Returns(true);
            _mockPlaywrightPage.Locator("html", Arg.Any<PageLocatorOptions>()).Returns(mockLocator);

            var component = new TestComponent(_mockPage);
            var result = await component.IsVisibleAsync();

            result.Should().BeTrue();
        }
    }
}
