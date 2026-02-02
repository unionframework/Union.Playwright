using FluentAssertions;
using NUnit.Framework;
using Union.Playwright.SCSS;

namespace Union.Playwright.Tests.SCSS;

[TestFixture]
public class ScssTests
{
    [TestCase("div", "div", "//div/descendant::div", "div div")]
    public void Concat(string scssSelector1, string scssSelector2, string resultXpath, string resultCss)
    {
        var scss1 = ScssBuilder.Create(scssSelector1);
        var scss2 = ScssBuilder.Create(scssSelector2);
        var resultScss = scss1.Concat(scss2);
        resultScss.Xpath.Should().Be(resultXpath);
        resultScss.Css.Should().Be(resultCss);
    }
}
