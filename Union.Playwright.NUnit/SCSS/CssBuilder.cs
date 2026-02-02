namespace Union.Playwright.NUnit.SCSS;

public class CssBuilder
{
    private const char CSS_PARTS_DELIMITER = ',';

    public static string Concat(string rootCss, string relativeCss)
    {
        if (string.IsNullOrWhiteSpace(relativeCss))
        {
            return rootCss;
        }
        if (string.IsNullOrEmpty(rootCss))
        {
            return relativeCss;
        }
        var roots = rootCss.Split(CSS_PARTS_DELIMITER);
        if (roots.Length == 1)
        {
            return string.Format("{0} {1}", rootCss, relativeCss);
        }
        var s = roots.Aggregate(
            string.Empty,
            (current, rootXpath) => current + Concat(rootXpath.Trim(), relativeCss) + ",");
        return s.Substring(0, s.Length - 1);
    }
}
