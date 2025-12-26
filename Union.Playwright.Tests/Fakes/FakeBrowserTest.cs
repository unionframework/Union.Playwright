using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace Union.Playwright.Tests.Fakes;

/// <summary>
/// A minimal concrete implementation of BrowserTest for unit testing.
/// This allows testing TestSessionProvider without starting real browsers.
/// </summary>
public class FakeBrowserTest : BrowserTest
{
    private readonly IBrowserContext _fakeContext;
    private readonly Func<IBrowserContext>? _contextFactory;

    public FakeBrowserTest(IBrowserContext fakeContext)
    {
        _fakeContext = fakeContext;
        _contextFactory = null;
    }

    public FakeBrowserTest(Func<IBrowserContext> contextFactory)
    {
        _fakeContext = null!;
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Hides the base NewContext method to return our fake context.
    /// Note: Uses 'new' keyword since base method is not virtual.
    /// </summary>
    public new Task<IBrowserContext> NewContext(BrowserNewContextOptions? options = null)
    {
        var context = _contextFactory?.Invoke() ?? _fakeContext;
        return Task.FromResult(context);
    }
}

/// <summary>
/// Extended fake that counts context creations to detect race conditions.
/// </summary>
public class CountingFakeBrowserTest : FakeBrowserTest
{
    private readonly Action _onNewContext;

    public CountingFakeBrowserTest(IBrowserContext fakeContext, Action onNewContext)
        : base(fakeContext)
    {
        _onNewContext = onNewContext;
    }

    public new Task<IBrowserContext> NewContext(BrowserNewContextOptions? options = null)
    {
        _onNewContext();
        return base.NewContext(options);
    }
}
