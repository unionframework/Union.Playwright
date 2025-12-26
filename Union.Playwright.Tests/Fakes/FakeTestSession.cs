using Union.Playwright.Core;
using Union.Playwright.Pages;
using Union.Playwright.Services;
using Union.Playwright.TestSession;

namespace Union.Playwright.Tests.Fakes;

/// <summary>
/// A fake page for testing purposes.
/// </summary>
public class FakeServicePage : UnionPage
{
    public override string AbsolutePath => "/fake";
}

/// <summary>
/// A working version of MyService that doesn't throw NotImplementedException.
/// We extend MyService to override the BaseUrl property.
/// </summary>
public class WorkingMyService : MyService
{
    public WorkingMyService(IServiceContextsPool serviceContextsPool) : base(serviceContextsPool)
    {
    }

    public override string BaseUrl => "https://test.example.com";
}

/// <summary>
/// A concrete TestSession implementation for testing.
/// Extends the abstract TestSession base class.
/// </summary>
public class FakeTestSession : Union.Playwright.TestSession.TestSession
{
    public FakeTestSession(MyService myService) : base(myService)
    {
    }
}
