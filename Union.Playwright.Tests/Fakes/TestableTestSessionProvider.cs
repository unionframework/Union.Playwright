using Microsoft.Extensions.DependencyInjection;
using Union.Playwright.Core;
using Union.Playwright.TestSession;

namespace Union.Playwright.Tests.Fakes;

/// <summary>
/// A concrete TestSessionProvider for testing purposes.
/// Registers WorkingMyService instead of the broken MyService.
/// </summary>
public class TestableTestSessionProvider : TestSessionProvider<FakeTestSession>
{
    public Action<IServiceCollection>? AdditionalServiceConfigurator { get; set; }

    public override void ConfigureServices(IServiceCollection services)
    {
        // Register WorkingMyService as MyService so DI works correctly
        services.AddTransient<MyService, WorkingMyService>();

        // Allow tests to add additional service registrations
        AdditionalServiceConfigurator?.Invoke(services);
    }
}
