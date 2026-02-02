using FluentAssertions;
using NUnit.Framework;
using Union.Playwright.SCSS;

namespace Union.Playwright.Tests.SCSS;

[TestFixture]
public class ScssBuilderTests
{
    [TestCase("div[~'text']", "//div[contains(text(),'text')]")]
    [TestCase("div['text']", "//div[text()='text']")]
    [TestCase("div[src='1.png']['text']", "//div[@src='1.png'][text()='text']")]
    [TestCase("div[src=\"1.png\"]['text']", "//div[@src=\"1.png\"][text()='text']")]
    [TestCase(".classname#myid['text']", "//*[@id='myid'][contains(@class,'classname')][text()='text']")]
    [TestCase(".classname['mytext']", "//*[contains(@class,'classname')][text()='mytext']")]
    [TestCase("div.classname['mytext']", "//div[contains(@class,'classname')][text()='mytext']")]
    [TestCase(".classname1.classname2['mytext']",
        "//*[contains(@class,'classname1')][contains(@class,'classname2')][text()='mytext']")]
    [TestCase("div.classname1.classname2['mytext']",
        "//div[contains(@class,'classname1')][contains(@class,'classname2')][text()='mytext']")]
    [TestCase(".classname1['mytext'] .classname2['mytext']",
        "//*[contains(@class,'classname1')][text()='mytext']/descendant::*[contains(@class,'classname2')][text()='mytext']")]
    [TestCase("div.classname1['mytext'] div.classname2['mytext']",
        "//div[contains(@class,'classname1')][text()='mytext']/descendant::div[contains(@class,'classname2')][text()='mytext']")]
    [TestCase("#myid div['mytext']", "//*[@id='myid']/descendant::div[text()='mytext']")]
    [TestCase("div#myid div['mytext']", "//div[@id='myid']/descendant::div[text()='mytext']")]
    [TestCase("div#myid.classname div['mytext']",
        "//div[@id='myid'][contains(@class,'classname')]/descendant::div[text()='mytext']")]
    [TestCase("div#main-basket-info-div>ul>li['Тариф']>a",
        "//div[@id='main-basket-info-div']/ul/li[text()='Тариф']/a")]
    [TestCase("li[>h5>strong>a['mytext']]", "//li[h5/strong/a[text()='mytext']]")]
    [TestCase("li[>a]", "//li[a]")]
    [TestCase("li[>a[div]]", "//li[a[descendant::div]]")]
    [TestCase("tr[1]>td[last()]", "//tr[1]/td[last()]")]
    [TestCase("img[src~'111.png']", "//img[contains(@src,'111.png')]")]
    [TestCase("#showThemesPanel,.genre-filter['text']",
        "//*[@id='showThemesPanel']|//*[contains(@class,'genre-filter')][text()='text']")]
    [TestCase(">div.toggle-drop>ul>li>span['Вечером']",
        "//child::div[contains(@class,'toggle-drop')]/ul/li/span[text()='Вечером']")]
    [TestCase("li[10]>div.news-block", "//li[10]/div[contains(@class,'news-block')]")]
    [TestCase("td[h3>span['Категории, на которые вы уже подписаны']]>div>div",
        "//td[descendant::h3/span[text()='Категории, на которые вы уже подписаны']]/div/div")]
    public void ConvertScssToXpath(string scssSelector, string result)
    {
        var scss = ScssBuilder.Create(scssSelector);
        scss.Xpath.Should().Be(result);
        scss.Css.Should().BeEmpty();
    }

    [TestCase("#myid", "#myid")]
    [TestCase("div#myid", "div#myid")]
    [TestCase("div#myid.classname", "div#myid.classname")]
    [TestCase(".classname", ".classname")]
    [TestCase("div.classname", "div.classname")]
    [TestCase(".classname1.classname2", ".classname1.classname2")]
    [TestCase("div.classname1.classname2", "div.classname1.classname2")]
    [TestCase(".classname1 .classname2", ".classname1 .classname2")]
    [TestCase("div.classname1 div.classname2", "div.classname1 div.classname2")]
    [TestCase("div[src='1.png']", "div[src='1.png']")]
    [TestCase("div[src=\"1.png\"]", "div[src=\"1.png\"]")]
    [TestCase(">.search-bar", ">.search-bar")]
    [TestCase(".nav-section >.search-bar", ".nav-section >.search-bar")]
    [TestCase(".nav-section >.search-bar ul", ".nav-section >.search-bar ul")]
    public void ConvertScssToCss(string scssSelector, string result)
    {
        var scss = ScssBuilder.Create(scssSelector);
        scss.Css.Should().Be(result);
        scss.Xpath.Should().NotBeEmpty();
    }
}
