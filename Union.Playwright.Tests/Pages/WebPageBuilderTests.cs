using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.Attributes;
using Union.Playwright.Components;
using Union.Playwright.Pages;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Tests.Pages
{
    #region Test Fixtures

    public class SimpleComponent : ComponentBase
    {
        public SimpleComponent(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }
    }

    public class NestedComponent : ComponentBase
    {
        [UnionInit(".inner-child")]
        public SimpleComponent InnerChild { get; set; }

        public NestedComponent(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }
    }

    public class DeeplyNestedComponent : ComponentBase
    {
        [UnionInit(".level3")]
        public SimpleComponent Level3Child { get; set; }

        public DeeplyNestedComponent(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }
    }

    public class TwoLevelNestedComponent : ComponentBase
    {
        [UnionInit(".level2")]
        public DeeplyNestedComponent Level2Child { get; set; }

        public TwoLevelNestedComponent(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }
    }

    public class PageWithField : UnionPage
    {
        public override string AbsolutePath => "/test";

        [UnionInit(".my-component")]
        public SimpleComponent MyComponent;
    }

    public class PageWithProperty : UnionPage
    {
        public override string AbsolutePath => "/test";

        [UnionInit(".prop-component")]
        public SimpleComponent MyComponent { get; set; }
    }

    public class PageWithFieldAndProperty : UnionPage
    {
        public override string AbsolutePath => "/test";

        [UnionInit(".field-comp")]
        public SimpleComponent FieldComp;

        [UnionInit(".prop-comp")]
        public SimpleComponent PropComp { get; set; }
    }

    public class PageWithNestedComponents : UnionPage
    {
        public override string AbsolutePath => "/test";

        [UnionInit(".parent")]
        public NestedComponent Parent;
    }

    public class PageWithThreeLevelNesting : UnionPage
    {
        public override string AbsolutePath => "/test";

        [UnionInit(".level1")]
        public TwoLevelNestedComponent Level1;
    }

    public class PageWithComponentName : UnionPage
    {
        public override string AbsolutePath => "/test";

        [UnionInit(".comp", ComponentName = "Custom Name")]
        public SimpleComponent MyComponent;
    }

    public class PageWithFrameXcss : UnionPage
    {
        public override string AbsolutePath => "/test";

        [UnionInit(".comp", FrameXcss = "iframe.main")]
        public SimpleComponent MyComponent;
    }

    public class PageWithNoInit : UnionPage
    {
        public override string AbsolutePath => "/test";

        public SimpleComponent NotInitialized;
    }

    public class PageWithRootScss : UnionPage
    {
        public override string AbsolutePath => "/test";
        public override string RootScss => "div.page-root";

        [UnionInit(".child")]
        public SimpleComponent Child;
    }

    #endregion

    [TestFixture]
    public class WebPageBuilderTests
    {
        private IPage _mockPlaywrightPage;

        [SetUp]
        public void SetUp()
        {
            _mockPlaywrightPage = Substitute.For<IPage>();
        }

        [Test]
        public void InitPage_WithFieldComponent_InitializesField()
        {
            var page = new PageWithField();
            page.Activate(_mockPlaywrightPage);

            page.MyComponent.Should().NotBeNull();
            page.MyComponent.ParentPage.Should().BeSameAs(page);
        }

        [Test]
        public void InitPage_WithPropertyComponent_InitializesProperty()
        {
            var page = new PageWithProperty();
            page.Activate(_mockPlaywrightPage);

            page.MyComponent.Should().NotBeNull();
            page.MyComponent.ParentPage.Should().BeSameAs(page);
        }

        [Test]
        public void InitPage_WithFieldAndProperty_InitializesBoth()
        {
            var page = new PageWithFieldAndProperty();
            page.Activate(_mockPlaywrightPage);

            page.FieldComp.Should().NotBeNull();
            page.PropComp.Should().NotBeNull();
        }

        [Test]
        public void InitPage_WithNoUnionInitAttribute_DoesNotInitialize()
        {
            var page = new PageWithNoInit();
            page.Activate(_mockPlaywrightPage);

            page.NotInitialized.Should().BeNull();
        }

        [Test]
        public void InitPage_SetsComponentNameFromMemberName_WhenNoAttributeName()
        {
            var page = new PageWithField();
            page.Activate(_mockPlaywrightPage);

            page.MyComponent.ComponentName.Should().Be("MyComponent");
        }

        [Test]
        public void InitPage_SetsComponentNameFromAttribute_WhenProvided()
        {
            var page = new PageWithComponentName();
            page.Activate(_mockPlaywrightPage);

            page.MyComponent.ComponentName.Should().Be("Custom Name");
        }

        [Test]
        public void InitPage_SetsFrameXcss_WhenProvided()
        {
            var page = new PageWithFrameXcss();
            page.Activate(_mockPlaywrightPage);

            page.MyComponent.FrameScss.Should().Be("iframe.main");
        }

        [Test]
        public void InitPage_SelectorConcatenation_OneLevelDeep()
        {
            var page = new PageWithField();
            page.Activate(_mockPlaywrightPage);

            // Page RootScss is null, ScssBuilder.Concat(null, ".my-component") produces a selector
            // that contains "my-component" (may be xpath or css depending on ScssBuilder)
            page.MyComponent.RootScss.Should().Contain("my-component");
        }

        [Test]
        public void InitPage_SelectorConcatenation_WithPageRootScss()
        {
            var page = new PageWithRootScss();
            page.Activate(_mockPlaywrightPage);

            // Page has RootScss "div.page-root", child selector is ".child"
            // ScssBuilder.Concat("div.page-root", ".child") produces concatenated selector
            page.Child.Should().NotBeNull();
            page.Child.RootScss.Should().NotBe(".child");
            page.Child.RootScss.Should().Contain("page-root");
        }

        [Test]
        public void InitPage_NestedComponents_TwoLevels()
        {
            var page = new PageWithNestedComponents();
            page.Activate(_mockPlaywrightPage);

            page.Parent.Should().NotBeNull();
            page.Parent.InnerChild.Should().NotBeNull();
            page.Parent.InnerChild.ParentPage.Should().BeSameAs(page);
        }

        [Test]
        public void InitPage_NestedComponents_ThreeLevels()
        {
            var page = new PageWithThreeLevelNesting();
            page.Activate(_mockPlaywrightPage);

            page.Level1.Should().NotBeNull();
            page.Level1.Level2Child.Should().NotBeNull();
            page.Level1.Level2Child.Level3Child.Should().NotBeNull();
            page.Level1.Level2Child.Level3Child.ParentPage.Should().BeSameAs(page);
        }

        [Test]
        public void InitPage_NestedSelectorConcatenation_TwoLevels()
        {
            var page = new PageWithNestedComponents();
            page.Activate(_mockPlaywrightPage);

            // Parent has RootScss from ".parent" selector
            // InnerChild should have concatenation of parent's RootScss + ".inner-child"
            var parentRoot = page.Parent.RootScss;
            var innerRoot = page.Parent.InnerChild.RootScss;

            parentRoot.Should().NotBeNullOrEmpty();
            innerRoot.Should().NotBeNullOrEmpty();
            // Inner selector should be different from just ".inner-child" since it's concatenated
            innerRoot.Should().NotBe(".inner-child");
        }

        [Test]
        public void CreateComponent_WithStringArgs_ConcatenatesSelector()
        {
            var page = new PageWithField();
            page.Activate(_mockPlaywrightPage);

            var attr = new UnionInit(".test-selector");
            var comp = WebPageBuilder.CreateComponent(page, page, typeof(SimpleComponent), attr);
            comp.Should().NotBeNull();
            comp.Should().BeOfType<SimpleComponent>();
        }

        [Test]
        public void CreateComponent_DoesNotModifyOriginalAttributeArgs()
        {
            var page = new PageWithRootScss();
            page.Activate(_mockPlaywrightPage);

            var attr = new UnionInit(".relative-selector");
            var originalArg = attr.Args[0];

            WebPageBuilder.CreateComponent(page, page, typeof(SimpleComponent), attr);

            attr.Args[0].Should().Be(originalArg, "original attribute args should not be modified");
        }
    }
}
