using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.Components;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Tests.Components
{
    public class TestItem : ItemBase
    {
        public TestItem(IContainer container, string id)
            : base(container, id)
        {
        }

        public override string ItemScss => $".item[data-id='{this.Id}']";
    }

    [TestFixture]
    public class ItemBaseTests
    {
        private IUnionPage _mockPage;
        private IContainer _mockContainer;

        [SetUp]
        public void SetUp()
        {
            _mockPage = Substitute.For<IUnionPage>();
            var mockPlaywrightPage = Substitute.For<IPage>();
            _mockPage.PlaywrightPage.Returns(mockPlaywrightPage);

            _mockContainer = Substitute.For<IContainer>();
            _mockContainer.ParentPage.Returns(_mockPage);
        }

        [Test]
        public void Constructor_SetsId()
        {
            var item = new TestItem(_mockContainer, "abc-123");

            item.Id.Should().Be("abc-123");
        }

        [Test]
        public void ParentPage_ReturnsContainerParentPage()
        {
            var item = new TestItem(_mockContainer, "abc-123");

            item.ParentPage.Should().BeSameAs(_mockPage);
        }

        [Test]
        public void RootScss_ReturnsItemScss()
        {
            var item = new TestItem(_mockContainer, "abc-123");

            item.RootScss.Should().Be(".item[data-id='abc-123']");
        }

        [Test]
        public void RootScss_IsLazyCached()
        {
            var item = new TestItem(_mockContainer, "abc-123");

            var first = item.RootScss;
            var second = item.RootScss;

            first.Should().BeSameAs(second);
        }
    }
}
