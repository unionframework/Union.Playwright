using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.Core;
using Union.Playwright.Services;
using Union.Playwright.TestSession;

namespace Union.Playwright.Tests.TestSession;

#region Test Doubles

/// <summary>
/// Minimal test session for verifying UnionTest DI wiring.
/// </summary>
public class StubTestSession : ITestSession
{
    private readonly List<IUnionService> _services;

    public StubTestSession(IEnumerable<IUnionService> services)
    {
        this._services = services.ToList();
    }

    public List<IUnionService> GetServices()
    {
        return this._services;
    }
}

/// <summary>
/// A TestSessionProvider for StubTestSession.
/// </summary>
public class StubTestSessionProvider : TestSessionProvider<StubTestSession>
{
    private static Action<IServiceCollection>? _pendingConfigurator;

    public StubTestSessionProvider() : this(null) { }

    public StubTestSessionProvider(Action<IServiceCollection>? configurator)
    {
        // Note: configurator must be set via static field before base ctor runs ConfigureServices
        _ = configurator; // already consumed via _pendingConfigurator
    }

    public static StubTestSessionProvider CreateWithServices(Action<IServiceCollection> configurator)
    {
        _pendingConfigurator = configurator;
        var provider = new StubTestSessionProvider(configurator);
        _pendingConfigurator = null;
        return provider;
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        if (_pendingConfigurator != null)
        {
            _pendingConfigurator.Invoke(services);
        }
        else
        {
            services.AddSingleton<IEnumerable<IUnionService>>(Array.Empty<IUnionService>());
        }
    }
}

/// <summary>
/// A concrete UnionTest for testing purposes.
/// Cannot run Playwright lifecycle (PageTest requires a real browser),
/// so we test the DI/lifecycle methods directly.
/// </summary>
public class TestableUnionTest : UnionTest<StubTestSession>
{
    private readonly StubTestSessionProvider _provider;

    public TestableUnionTest() : this(new StubTestSessionProvider()) { }

    public TestableUnionTest(StubTestSessionProvider provider)
    {
        this._provider = provider;
    }

    /// <summary>
    /// Expose the protected Session property for testing.
    /// </summary>
    public new StubTestSession Session => base.Session;

    protected override TestSessionProvider<StubTestSession> GetSessionProvider() => this._provider;
}

#endregion

[TestFixture]
public class UnionTestTests
{
    #region SetUp / TearDown Tests

    [Test]
    public void UnionSetUp_ResolvesSession()
    {
        // Arrange
        var sut = new TestableUnionTest();

        // Act
        sut.UnionSetUp();

        // Assert
        sut.Session.Should().NotBeNull();
        sut.Session.Should().BeOfType<StubTestSession>();

        // Cleanup
        sut.UnionTearDown();
    }

    [Test]
    public void UnionTearDown_WithoutSetUp_DoesNotThrow()
    {
        // Arrange
        var sut = new TestableUnionTest();

        // Act
        var act = () => sut.UnionTearDown();

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void UnionSetUp_CalledTwice_CreatesFreshSession()
    {
        // Arrange
        var sut = new TestableUnionTest();

        // Act
        sut.UnionSetUp();
        var firstSession = sut.Session;
        sut.UnionTearDown();

        sut.UnionSetUp();
        var secondSession = sut.Session;
        sut.UnionTearDown();

        // Assert - each SetUp creates a new scope and session
        firstSession.Should().NotBeSameAs(secondSession);
    }

    #endregion

    #region GetSessionProvider Tests

    [Test]
    public void GetSessionProvider_CustomRegistrations_AreAvailable()
    {
        // Arrange
        var mockService = Substitute.For<IUnionService>();
        var provider = StubTestSessionProvider.CreateWithServices(services =>
        {
            services.AddSingleton<IEnumerable<IUnionService>>(new[] { mockService });
        });
        var sut = new TestableUnionTest(provider);

        // Act
        sut.UnionSetUp();

        // Assert
        var session = sut.Session;
        session.GetServices().Should().Contain(mockService);

        // Cleanup
        sut.UnionTearDown();
    }

    [Test]
    public void GetSessionProvider_WithNoCustomServices_SessionHasEmptyServiceList()
    {
        // Arrange
        var sut = new TestableUnionTest();

        // Act
        sut.UnionSetUp();

        // Assert
        sut.Session.GetServices().Should().BeEmpty();

        // Cleanup
        sut.UnionTearDown();
    }

    #endregion

    #region GetService Tests

    [Test]
    public void GetService_ReturnsMatchingService()
    {
        // Arrange
        var mockService = Substitute.For<IUnionService>();
        var provider = StubTestSessionProvider.CreateWithServices(services =>
        {
            services.AddSingleton<IEnumerable<IUnionService>>(new[] { mockService });
        });
        var sut = new TestableUnionTest(provider);
        sut.UnionSetUp();

        // Act
        var service = sut.GetServicePublic<IUnionService>();

        // Assert
        service.Should().BeSameAs(mockService);

        // Cleanup
        sut.UnionTearDown();
    }

    [Test]
    public void GetService_WhenNoMatchingService_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = new TestableUnionTest();
        sut.UnionSetUp();

        // Act
        var act = () => sut.GetServicePublic<IUnionService>();

        // Assert - reflection wraps the exception in TargetInvocationException
        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<InvalidOperationException>();

        // Cleanup
        sut.UnionTearDown();
    }

    #endregion

    #region TestAwareServiceContextsPool SetPageFactory Tests

    [Test]
    public async Task SetPageFactory_PoolUsesPageContext()
    {
        // Arrange
        var mockContext = Substitute.For<IBrowserContext>();
        var mockPage = Substitute.For<IPage>();
        mockPage.Context.Returns(mockContext);

        var pool = new TestAwareServiceContextsPool();
        pool.SetPageFactory(() => mockPage);

        var service = Substitute.For<IUnionService>();

        // Act
        var context = await pool.GetContext(service);

        // Assert
        context.Should().BeSameAs(mockContext);
    }

    [Test]
    public void GetContext_WithoutSetup_ThrowsInvalidOperationException()
    {
        // Arrange
        var pool = new TestAwareServiceContextsPool();
        var service = Substitute.For<IUnionService>();

        // Act
        Func<Task> act = async () => await pool.GetContext(service);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*page factory*");
    }

    #endregion
}

/// <summary>
/// Extension to expose GetService for testing (it is protected).
/// </summary>
public static class TestableUnionTestExtensions
{
    public static TService GetServicePublic<TService>(this TestableUnionTest test)
        where TService : IUnionService
    {
        // Use reflection to call the protected method
        var method = typeof(UnionTest<StubTestSession>)
            .GetMethod("GetService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(typeof(TService));
        return (TService)method.Invoke(test, null)!;
    }
}
