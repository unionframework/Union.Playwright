using System.Xml.XPath;

namespace Union.Playwright.NUnit.SCSS;

public class XPathBuilder
{
    public const string DESCENDANT_AXIS = "descendant::";
    public const string ANCESTOR_AXIS = "ancestor::";
    public const string ANCESTORORSELF_AXIS = "ancestor-or-self::";
    public const string ATTRIBUTE_AXIS = "attribute::";
    public const string CHILD_AXIS = "child::";
    public const string DESCENDANTORSELF_AXIS = "descendant-or-self::";
    public const string FOLLOWING_AXIS = "following::";
    public const string FOLLOWINGSIBLING_AXIS = "following-sibling::";
    public const string NAMESPACE_AXIS = "namespace::";
    public const string PARENT_AXIS = "parent::";
    public const string PRECEDING_AXIS = "preceding::";
    public const string PRECEDINGSIBLING_AXIS = "preceding-sibling::";
    public const string SELF_AXIS = "self::";

    private const string XPATH_ROOT = "//";

    public static string Concat(string root, string relative, params object[] args)
    {
        relative = string.Format(relative, args);
        if (relative.StartsWith(XPATH_ROOT))
        {
            relative = relative.Substring(2, relative.Length - 2);
        }
        if (string.IsNullOrWhiteSpace(relative))
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new Exception("Invalid xpath: root and relative parts are empty");
            }
            return root;
        }
        if (string.IsNullOrWhiteSpace(root))
        {
            return XPATH_ROOT + relative;
        }
        var rootXpaths = root.Split('|');
        if (rootXpaths.Length == 1)
        {
            var axis = HasAxis(relative) ? string.Empty : DESCENDANT_AXIS;
            return string.Format("{0}/{1}{2}", root, axis, relative);
        }
        var s = rootXpaths.Aggregate(
            string.Empty,
            (current, rootXpath) => current + Concat(rootXpath.Trim(), relative) + "|");
        return s.Substring(0, s.Length - 1);
    }

    private static bool HasAxis(string xpath)
    {
        var axises = new List<string>
        {
            ANCESTOR_AXIS,
            ANCESTORORSELF_AXIS,
            ATTRIBUTE_AXIS,
            CHILD_AXIS,
            DESCENDANT_AXIS,
            DESCENDANTORSELF_AXIS,
            FOLLOWING_AXIS,
            FOLLOWINGSIBLING_AXIS,
            NAMESPACE_AXIS,
            PARENT_AXIS,
            PRECEDING_AXIS,
            PRECEDINGSIBLING_AXIS,
            SELF_AXIS
        };
        return axises.Any(xpath.StartsWith);
    }

    public static bool IsXPath(string value)
    {
        try
        {
            XPathExpression.Compile(value);
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }
}
