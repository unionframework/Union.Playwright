using Microsoft.Extensions.DependencyInjection;
using Union.Playwright.Core;
using Union.Playwright.Services;

namespace Union.Playwright.Tests.Fakes;

/// <summary>
/// A concrete TestSessionProvider for testing purposes.
/// </summary>
public class TestableTestSessionProvider : TestSessionProvider<FakeTestSession>
{
    public Action<IServiceCollection>? AdditionalServiceConfigurator { get; set; }

    public override void ConfigureServices(IServiceCollection services)
    {
        // Register an empty set of IUnionService by default
        services.AddSingleton<IEnumerable<IUnionService>>(Array.Empty<IUnionService>());

        // Allow tests to add additional service registrations
        AdditionalServiceConfigurator?.Invoke(services);
    }
}
