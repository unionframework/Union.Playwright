using Union.Playwright.Core;
using Union.Playwright.Pages;
using Union.Playwright.Services;

namespace Union.Playwright.Tests.Fakes;

/// <summary>
/// A fake page for testing purposes.
/// </summary>
public class FakeServicePage : UnionPage
{
    public override string AbsolutePath => "/fake";
}

/// <summary>
/// A concrete ITestSession implementation for testing.
/// </summary>
public class FakeTestSession : ITestSession
{
    private readonly IEnumerable<IUnionService> _services;

    public FakeTestSession(IEnumerable<IUnionService> services)
    {
        this._services = services;
    }

    public List<IUnionService> GetServices()
    {
        return this._services.ToList();
    }
}
