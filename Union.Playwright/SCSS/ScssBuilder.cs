using System.Text.RegularExpressions;

namespace Union.Playwright.SCSS;

public class ScssBuilder
{
    private const string DESCENDANT_AXIS = "descendant::";
    private const string CHILD_AXIS = "child::";

    private static readonly Regex _identRegex = new Regex("\\A-?[_a-zA-Z][_a-zA-Z0-9-]*\\z", RegexOptions.Compiled);
    private static readonly Regex _nameRegex = new Regex("\\A[_a-zA-Z0-9-]+\\z", RegexOptions.Compiled);
    private static readonly Regex _numberRegex = new Regex("\\A\\d+\\z", RegexOptions.Compiled);

    public static Scss Create(string scssSelector)
    {
        if (string.IsNullOrEmpty(scssSelector))
        {
            return new Scss(string.Empty, string.Empty);
        }
        var xpath = string.Empty;
        string? css = null;
        try
        {
            var isTrueCss = true;
            var parts = SplitIgnoringConditions(scssSelector, true, ',');
            foreach (var partScssSelector in parts)
            {
                bool partScssIsTrueCss;
                var xpathPart = ScssPartToXpath(partScssSelector, out partScssIsTrueCss);
                xpath += "//" + RemoveDescendantAxis(xpathPart) + "|";
                isTrueCss &= partScssIsTrueCss;
            }
            xpath = xpath.Substring(0, xpath.Length - 1);
            if (isTrueCss)
            {
                css = scssSelector;
            }
        }
        catch (InvalidScssException)
        {
            if (XPathBuilder.IsXPath(scssSelector))
            {
                xpath = scssSelector;
            }
            else
            {
                throw;
            }
        }
        return new Scss(xpath, css);
    }

    public static Scss Concat(string scssSelector1, string scssSelector2)
    {
        var scss1 = Create(scssSelector1);
        var scss2 = Create(scssSelector2);
        return scss1.Concat(scss2);
    }

    private static List<string> SplitIgnoringConditions(
        string scssSelector,
        bool cutDelimiters,
        params char[] delimiters)
    {
        var scssParts = new List<string>();
        var value = string.Empty;
        var readCondition = false;
        var conditionOpenBracketsCount = 0;
        for (var i = 0; i < scssSelector.Length; i++)
        {
            var c = scssSelector[i];
            if (readCondition)
            {
                if (IsClosingConditionBracket(ref conditionOpenBracketsCount, c))
                {
                    readCondition = false;
                }
            }
            else if (delimiters.Contains(c))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    scssParts.Add(value);
                }
                value = string.Empty;
                if (cutDelimiters)
                {
                    continue;
                }
            }
            else if (c == '[')
            {
                readCondition = true;
            }
            value += c;
        }
        if (string.IsNullOrEmpty(value) || readCondition)
        {
            throw new InvalidScssException("SplitIgnoringConditions. unexpected end of line");
        }
        scssParts.Add(value);
        return scssParts;
    }

    private static string ScssPartToXpath(string scssPart, out bool isTrueCss)
    {
        var elementScssSelectors = SplitIgnoringConditions(scssPart, false, ' ', '>', '+');
        var xpath = string.Empty;
        isTrueCss = true;
        for (var i = 0; i < elementScssSelectors.Count; i++)
        {
            var elementScssSelector = elementScssSelectors[i];
            bool elementScssIsTrueCss;
            var elementXpath = ElementScssToXpath(elementScssSelector, out elementScssIsTrueCss);
            if (i > 0)
            {
                elementXpath = "/" + RemoveChildAxis(elementXpath);
            }
            xpath += elementXpath;
            isTrueCss &= elementScssIsTrueCss;
        }
        return xpath;
    }

    private static string RemoveDescendantAxis(string elementXpath)
    {
        if (elementXpath.StartsWith(DESCENDANT_AXIS))
        {
            elementXpath = elementXpath.Remove(0, DESCENDANT_AXIS.Length);
        }
        return elementXpath;
    }

    private static string RemoveChildAxis(string elementXpath)
    {
        if (elementXpath.StartsWith(CHILD_AXIS))
        {
            elementXpath = elementXpath.Remove(0, CHILD_AXIS.Length);
        }
        return elementXpath;
    }

    private static string ElementScssToXpath(string elementScss, out bool isTrueCss)
    {
        if (string.IsNullOrWhiteSpace(elementScss))
        {
            throw new InvalidScssException("Invalid scss: {0}", elementScss);
        }
        var combinator = string.Empty;
        switch (elementScss[0])
        {
            case ' ':
            case '>':
                combinator = elementScss[0].ToString();
                elementScss = elementScss.Substring(1);
                break;
        }
        var tag = string.Empty;
        var id = string.Empty;
        var className = string.Empty;
        var condition = string.Empty;
        var classNames = new List<string>();
        var attributes = new List<ScssAttribute>();
        var conditions = new List<string>();
        var subelementXpaths = new List<string>();
        var state = State.ReadTag;
        var conditionOpenBracketsCount = 0;
        for (var i = 0; i < elementScss.Length; i++)
        {
            var c = elementScss[i];
            if (state == State.ReadCondition && !IsClosingConditionBracket(ref conditionOpenBracketsCount, c))
            {
                condition += c;
                continue;
            }
            switch (c)
            {
                case '.':
                    switch (state)
                    {
                        case State.ReadClass:
                            if (string.IsNullOrEmpty(className))
                            {
                                ThrowIncorrectSymbol(state, i, elementScss);
                            }
                            classNames.Add(className);
                            className = string.Empty;
                            break;
                        case State.ReadId:
                            if (string.IsNullOrEmpty(id))
                            {
                                ThrowIncorrectSymbol(state, i, elementScss);
                            }
                            break;
                        case State.ReadTag:
                        case State.Undefined:
                            break;
                        default:
                            ThrowIncorrectSymbol(state, i, elementScss);
                            break;
                    }
                    state = State.ReadClass;
                    break;
                case '#':
                    if (!string.IsNullOrEmpty(id))
                    {
                        throw new InvalidScssException("two ids are illegal");
                    }
                    switch (state)
                    {
                        case State.ReadClass:
                            if (string.IsNullOrEmpty(className))
                            {
                                ThrowIncorrectSymbol(state, i, elementScss);
                            }
                            classNames.Add(className);
                            className = string.Empty;
                            break;
                        case State.ReadTag:
                        case State.Undefined:
                            break;
                        default:
                            ThrowIncorrectSymbol(state, i, elementScss);
                            break;
                    }
                    state = State.ReadId;
                    break;
                case '[':
                    switch (state)
                    {
                        case State.ReadClass:
                            if (string.IsNullOrEmpty(className))
                            {
                                ThrowIncorrectSymbol(state, i, elementScss);
                            }
                            classNames.Add(className);
                            className = string.Empty;
                            break;
                        case State.ReadId:
                            if (string.IsNullOrEmpty(id))
                            {
                                ThrowIncorrectSymbol(state, i, elementScss);
                            }
                            break;
                        case State.ReadTag:
                        case State.Undefined:
                            break;
                        default:
                            ThrowIncorrectSymbol(state, i, elementScss);
                            break;
                    }
                    state = State.ReadCondition;
                    break;
                case ']':
                    if (state != State.ReadCondition)
                    {
                        ThrowIncorrectSymbol(state, i, elementScss);
                    }
                    if (IsText(condition) || IsNumber(condition) || IsFunction(condition))
                    {
                        conditions.Add(condition);
                    }
                    else
                    {
                        var attribute = ParseAttribute(condition);
                        if (attribute != null)
                        {
                            attributes.Add(attribute);
                        }
                        else
                        {
                            bool dummyBool;
                            var xpathPart = ScssPartToXpath(condition, out dummyBool);
                            subelementXpaths.Add(RemoveChildAxis(xpathPart));
                        }
                    }
                    condition = string.Empty;
                    state = State.Undefined;
                    break;
                default:
                    switch (state)
                    {
                        case State.ReadId:
                            id += c;
                            break;
                        case State.ReadTag:
                            tag += c;
                            break;
                        case State.ReadClass:
                            className += c;
                            break;
                        case State.ReadCondition:
                            condition += c;
                            break;
                        case State.Undefined:
                            ThrowIncorrectSymbol(state, i, elementScss);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
            }
        }
        switch (state)
        {
            case State.Undefined:
            case State.ReadId:
            case State.ReadTag:
                break;
            case State.ReadClass:
                if (string.IsNullOrEmpty(className))
                {
                    ThrowIncorrectSymbol(state, elementScss.Length, elementScss);
                }
                classNames.Add(className);
                break;
            case State.ReadCondition:
                if (string.IsNullOrEmpty(className))
                {
                    ThrowIncorrectSymbol(state, elementScss.Length, elementScss);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        isTrueCss = conditions.Count == 0 && subelementXpaths.Count == 0
                    && attributes.All(a => IsCssMatchStyle(a.MatchStyle));
        Validate(tag, id, classNames, attributes);
        return AggregateToXpath(combinator, tag, id, classNames, attributes, conditions, subelementXpaths);
    }

    private static bool IsCssMatchStyle(AttributeMatchStyle matchStyle)
    {
        return matchStyle switch
        {
            AttributeMatchStyle.Equal => true,
            _ => false
        };
    }

    private static ScssAttribute? ParseAttribute(string condition)
    {
        foreach (AttributeMatchStyle matchStyle in Enum.GetValues(typeof(AttributeMatchStyle)))
        {
            string[] arr = condition.Split(
                new[] { matchStyle.StringValue() },
                StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length == 2 && IsText(arr[1]))
            {
                return new ScssAttribute(arr[0], arr[1], matchStyle);
            }
        }
        return null;
    }

    private static bool IsFunction(string condition)
    {
        return condition switch
        {
            "last()" => true,
            _ => false
        };
    }

    private static bool IsNumber(string condition)
    {
        return _numberRegex.IsMatch(condition);
    }

    private static void Validate(string tag, string id, List<string> classNames, List<ScssAttribute> attributes)
    {
        if (!string.IsNullOrEmpty(tag))
        {
            ValidateIsElementName(tag);
        }
        if (!string.IsNullOrEmpty(id))
        {
            ValidateIsName(id);
        }
        foreach (var className in classNames)
        {
            ValidateIsIdent(className);
        }
        foreach (var attribute in attributes)
        {
            ValidateIsIdent(attribute.Name);
        }
    }

    private static void ValidateIsName(string value)
    {
        if (!IsName(value))
        {
            throw new InvalidScssException("'{0}' is not name", value);
        }
    }

    private static void ValidateIsIdent(string value)
    {
        if (!IsIdent(value))
        {
            throw new InvalidScssException("'{0}' is not ident", value);
        }
    }

    private static void ValidateIsElementName(string value)
    {
        if (!IsElementName(value))
        {
            throw new InvalidScssException("'{0}' is not element name", value);
        }
    }

    private static bool IsIdent(string value)
    {
        return _identRegex.IsMatch(value);
    }

    private static bool IsElementName(string value)
    {
        return value == "*" || IsIdent(value);
    }

    private static bool IsName(string value)
    {
        return _nameRegex.IsMatch(value);
    }

    private static bool IsClosingConditionBracket(ref int conditionOpenBracketsCount, char c)
    {
        if (c == '[')
        {
            conditionOpenBracketsCount++;
        }
        else if (c == ']')
        {
            if (conditionOpenBracketsCount == 0)
            {
                return true;
            }
            conditionOpenBracketsCount--;
        }
        return false;
    }

    private static string AggregateToXpath(
        string axis,
        string tag,
        string id,
        List<string> classNames,
        List<ScssAttribute> attributes,
        List<string> conditions,
        List<string> subelementXpaths)
    {
        tag = string.IsNullOrEmpty(tag) ? "*" : tag;
        var xpath = XpathAxis(axis) + tag;
        if (!string.IsNullOrEmpty(id))
        {
            xpath += XpathAttributeCondition("id", string.Format("'{0}'", id));
        }
        foreach (var className in classNames)
        {
            xpath += XpathAttributeCondition(
                "class",
                string.Format("'{0}'", className),
                AttributeMatchStyle.Contains);
        }
        foreach (var attribute in attributes)
        {
            xpath += XpathAttributeCondition(attribute.Name, attribute.Value, attribute.MatchStyle);
        }
        foreach (var condition in conditions)
        {
            if (IsText(condition))
            {
                xpath += XpathTextCondition(condition);
            }
            else
            {
                xpath += XpathCondition(condition);
            }
        }
        foreach (var subelementXpath in subelementXpaths)
        {
            xpath += XpathCondition(subelementXpath);
        }
        return xpath;
    }

    private static string XpathCondition(string condition)
    {
        return string.Format("[{0}]", condition);
    }

    private static string XpathAxis(string axis)
    {
        return axis switch
        {
            "" or " " => DESCENDANT_AXIS,
            ">" => CHILD_AXIS,
            _ => throw new ArgumentOutOfRangeException(nameof(axis))
        };
    }

    private static string XpathTextCondition(string text)
    {
        if (text.StartsWith("~"))
        {
            text = text.StartsWith("~") ? text.Substring(1) : text;
            return string.Format("[contains(text(),{0})]", text);
        }
        return string.Format("[text()={0}]", text);
    }

    private static string XpathAttributeCondition(
        string name,
        string value,
        AttributeMatchStyle style = AttributeMatchStyle.Equal)
    {
        return style switch
        {
            AttributeMatchStyle.Equal => string.Format("[@{0}={1}]", name, value),
            AttributeMatchStyle.Contains => string.Format("[contains(@{0},{1})]", name, value),
            _ => throw new ArgumentOutOfRangeException(nameof(style))
        };
    }

    private static bool IsText(string stringValue)
    {
        stringValue = stringValue.StartsWith("~") ? stringValue.Substring(1) : stringValue;
        return stringValue.Length > 1
               && ((stringValue.StartsWith("'") && stringValue.EndsWith("'"))
                   || (stringValue.StartsWith("\"") && stringValue.EndsWith("\"")));
    }

    private static void ThrowIncorrectSymbol(State state, int index, string scss)
    {
        throw new InvalidScssException(
            "incorrect symbol for state: state: {0}, index: {1}, scss: {2}",
            state,
            index,
            scss);
    }

    private enum State
    {
        Undefined,
        ReadId,
        ReadTag,
        ReadClass,
        ReadCondition
    }
}
