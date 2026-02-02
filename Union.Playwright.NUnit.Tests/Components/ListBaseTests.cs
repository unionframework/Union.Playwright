using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Tests.Components
{
    public class TestListItem : ItemBase
    {
        public TestListItem(IContainer container, string id)
            : base(container, id)
        {
        }

        public override string ItemScss => $".row[data-id='{this.Id}']";
    }

    public class TestList : ListBase<TestListItem>
    {
        public TestList(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }

        public override string ItemIdScss => ".row";

        public override string IdAttribute => "data-id";
    }

    public class TestListTextContent : ListBase<TestListItem>
    {
        public TestListTextContent(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }

        public override string ItemIdScss => ".row";
    }

    [TestFixture]
    public class ListBaseTests
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
        public void CreateItem_CreatesItemWithCorrectId()
        {
            var list = new TestList(_mockPage, ".list");

            var item = list.CreateItem("item-1");

            item.Should().NotBeNull();
            item.Id.Should().Be("item-1");
        }

        [Test]
        public void CreateItem_ItemHasCorrectParentPage()
        {
            var list = new TestList(_mockPage, ".list");

            var item = list.CreateItem("item-1");

            item.ParentPage.Should().BeSameAs(_mockPage);
        }

        [Test]
        public void IdAttribute_ReturnsConfiguredValue()
        {
            var list = new TestList(_mockPage);

            list.IdAttribute.Should().Be("data-id");
        }

        [Test]
        public void IdAttribute_DefaultsToNull()
        {
            var list = new TestListTextContent(_mockPage);

            list.IdAttribute.Should().BeNull();
        }

        [Test]
        public async Task GetIdsAsync_WithAttribute_ReturnsAttributeValues()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(2);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0.GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>()).Returns("id-a");
            var mockElement1 = Substitute.For<ILocator>();
            mockElement1.GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>()).Returns("id-b");

            mockLocator.Nth(0).Returns(mockElement0);
            mockLocator.Nth(1).Returns(mockElement1);

            _mockPlaywrightPage.Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>()).Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var ids = await list.GetIdsAsync();

            ids.Should().BeEquivalentTo(new[] { "id-a", "id-b" });
        }

        [Test]
        public async Task GetIdsAsync_WithNullAttribute_UsesTextContent()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(1);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0.TextContentAsync(Arg.Any<LocatorTextContentOptions>()).Returns("text-val");

            mockLocator.Nth(0).Returns(mockElement0);

            _mockPlaywrightPage.Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>()).Returns(mockLocator);

            var list = new TestListTextContent(_mockPage, ".list");
            var ids = await list.GetIdsAsync();

            ids.Should().BeEquivalentTo(new[] { "text-val" });
        }

        [Test]
        public async Task GetItemsAsync_ReturnsItemsForEachId()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(2);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0.GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>()).Returns("id-1");
            var mockElement1 = Substitute.For<ILocator>();
            mockElement1.GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>()).Returns("id-2");

            mockLocator.Nth(0).Returns(mockElement0);
            mockLocator.Nth(1).Returns(mockElement1);

            _mockPlaywrightPage.Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>()).Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var items = await list.GetItemsAsync();

            items.Should().HaveCount(2);
            items[0].Id.Should().Be("id-1");
            items[1].Id.Should().Be("id-2");
        }

        [Test]
        public async Task FindSingleAsync_ReturnsFirstItem()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(2);

            var mockElement0 = Substitute.For<ILocator>();
            mockElement0.GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>()).Returns("first");
            var mockElement1 = Substitute.For<ILocator>();
            mockElement1.GetAttributeAsync("data-id", Arg.Any<LocatorGetAttributeOptions>()).Returns("second");

            mockLocator.Nth(0).Returns(mockElement0);
            mockLocator.Nth(1).Returns(mockElement1);

            _mockPlaywrightPage.Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>()).Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var item = await list.FindSingleAsync();

            item.Should().NotBeNull();
            item!.Id.Should().Be("first");
        }

        [Test]
        public async Task FindSingleAsync_WhenEmpty_ReturnsNull()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(0);

            _mockPlaywrightPage.Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>()).Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var item = await list.FindSingleAsync();

            item.Should().BeNull();
        }

        [Test]
        public async Task FindRandomAsync_WhenEmpty_ReturnsNull()
        {
            var mockLocator = Substitute.For<ILocator>();
            mockLocator.CountAsync().Returns(0);

            _mockPlaywrightPage.Locator(Arg.Any<string>(), Arg.Any<PageLocatorOptions>()).Returns(mockLocator);

            var list = new TestList(_mockPage, ".list");
            var item = await list.FindRandomAsync();

            item.Should().BeNull();
        }
    }
}
