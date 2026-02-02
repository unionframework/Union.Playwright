using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.Core;
using Union.Playwright.TestSession;
using Union.Playwright.Tests.Fakes;

namespace Union.Playwright.Tests.TestSession;

[TestFixture]
public class TestSessionProviderTests
{
    private TestableTestSessionProvider _provider = null!;
    private IPage _fakePage = null!;

    [SetUp]
    public void SetUp()
    {
        var fakeContext = Substitute.For<IBrowserContext>();
        _fakePage = Substitute.For<IPage>();
        _fakePage.Context.Returns(fakeContext);
        _provider = new TestableTestSessionProvider();
    }

    #region CreateTestSession Tests

    [Test]
    public void CreateTestSession_ReturnsNonNullScopedSession()
    {
        // Act
        using var scoped = _provider.CreateTestSession(() => _fakePage);

        // Assert
        scoped.Should().NotBeNull();
        scoped.Session.Should().NotBeNull();
        scoped.Session.Should().BeOfType<FakeTestSession>();
    }

    [Test]
    public void CreateTestSession_CalledTwice_ReturnsDifferentSessions()
    {
        // Act
        using var scoped1 = _provider.CreateTestSession(() => _fakePage);
        using var scoped2 = _provider.CreateTestSession(() => _fakePage);

        // Assert
        scoped1.Session.Should().NotBeSameAs(scoped2.Session);
    }

    [Test]
    public void CreateTestSession_SessionContainsRegisteredServices()
    {
        // Act
        using var scoped = _provider.CreateTestSession(() => _fakePage);
        var services = scoped.Session.GetServices();

        // Assert
        services.Should().NotBeNull();
    }

    [Test]
    public void CreateTestSession_DisposingScope_DoesNotThrow()
    {
        // Act
        var scoped = _provider.CreateTestSession(() => _fakePage);

        // Assert
        var act = () => scoped.Dispose();
        act.Should().NotThrow();
    }

    #endregion

    #region Session Lifecycle Tests

    [Test]
    public void CreateTestSession_MultipleCalls_EachGetsIsolatedSession()
    {
        // Act
        var sessions = Enumerable.Range(0, 5)
            .Select(_ => _provider.CreateTestSession(() => _fakePage))
            .ToList();

        try
        {
            // Assert
            var sessionInstances = sessions.Select(s => s.Session).ToList();
            sessionInstances.Should().OnlyHaveUniqueItems("each call should create a unique session");
        }
        finally
        {
            sessions.ForEach(s => s.Dispose());
        }
    }

    #endregion
}
