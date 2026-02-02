using System;

namespace Union.Playwright.NUnit.SCSS;

internal static class AttributeMatchStyleExtensions
{
    public static string StringValue(this AttributeMatchStyle style) => style switch
    {
        AttributeMatchStyle.Equal => "=",
        AttributeMatchStyle.Contains => "~",
        _ => throw new ArgumentOutOfRangeException(nameof(style))
    };
}
