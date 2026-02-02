namespace Union.Playwright.NUnit.SCSS;

internal class ScssAttribute
{
    public AttributeMatchStyle MatchStyle;
    public string Name;
    public string Value;

    public ScssAttribute(string name, string value, AttributeMatchStyle matchStyle)
    {
        Name = name;
        Value = value;
        MatchStyle = matchStyle;
    }
}
