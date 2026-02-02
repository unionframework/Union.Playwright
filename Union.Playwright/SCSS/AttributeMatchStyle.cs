namespace Union.Playwright.SCSS;

internal enum AttributeMatchStyle
{
    Equal,
    Contains
}

internal static class AttributeMatchStyleExtensions
{
    public static string StringValue(this AttributeMatchStyle style) => style switch
    {
        AttributeMatchStyle.Equal => "=",
        AttributeMatchStyle.Contains => "~",
        _ => throw new ArgumentOutOfRangeException(nameof(style))
    };
}
