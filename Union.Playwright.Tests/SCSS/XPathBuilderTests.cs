using FluentAssertions;
using NUnit.Framework;
using Union.Playwright.SCSS;

namespace Union.Playwright.Tests.SCSS;

[TestFixture]
public class XPathBuilderTests
{
    [TestCase("div", true)]
    [TestCase("//div", true)]
    [TestCase("//div[@id='myId']", true)]
    [TestCase("//div[text()='mytext']", true)]
    [TestCase("//div[text()='mytext' and @class='myclass']", true)]
    [TestCase("//div[@id='myId']/descendant::span", true)]
    [TestCase("//div[@id='myId1']|//div[@id='myId2']", true)]
    [TestCase("#myId", false)]
    [TestCase(".myclass", false)]
    public void IsXpath(string xpath, bool isXpath)
    {
        XPathBuilder.IsXPath(xpath).Should().Be(isXpath);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    public void RootIsEmpty(string root)
    {
        var relative = "div";
        XPathBuilder.Concat(root, relative).Should().Be("//div");
    }

    [Test]
    public void ConcatAsDescendant()
    {
        var root = "//div";
        var relative = "*[@id='myid']";
        XPathBuilder.Concat(root, relative).Should().Be("//div/descendant::*[@id='myid']");
    }

    [Test]
    public void InsertArgsToRelative()
    {
        var root = "//div";
        var relative = "*[@id='{0}']";
        XPathBuilder.Concat(root, relative, "myid").Should().Be("//div/descendant::*[@id='myid']");

        root = "//div[@id='{0}']";
        relative = "*[@id='{0}']";
        XPathBuilder.Concat(root, relative, "myid").Should().Be("//div[@id='{0}']/descendant::*[@id='myid']");
    }

    [Test]
    public void LeaveAxis()
    {
        var root = "//div";
        var relative = "self::*[@id='myid']";
        XPathBuilder.Concat(root, relative).Should().Be("//div/self::*[@id='myid']");
    }

    [Test]
    public void MakeRelative()
    {
        var root = "//*[@id='aaa1']";
        var relative = "//*[@id='bbb']";
        XPathBuilder.Concat(root, relative).Should().Be("//*[@id='aaa1']/descendant::*[@id='bbb']");
    }

    [Test]
    public void MultipleRootXpath()
    {
        var root = "//*[@id='aaa1'] | //*[@id='aaa2']";
        var relative = "*[@id='bbb']";
        XPathBuilder.Concat(root, relative, "myid").Should().Be(
            "//*[@id='aaa1']/descendant::*[@id='bbb']|//*[@id='aaa2']/descendant::*[@id='bbb']");
    }

    [Test]
    public void RelativeIsEmpty()
    {
        var root = "//*[@id='aaa1']";
        var relative = "";
        XPathBuilder.Concat(root, relative).Should().Be("//*[@id='aaa1']");
    }
}
