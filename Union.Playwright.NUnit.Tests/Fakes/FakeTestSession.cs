using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Pages;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Tests.Fakes;

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
        _services = services;
    }

    public List<IUnionService> GetServices()
    {
        return _services.ToList();
    }
}
