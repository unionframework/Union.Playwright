using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.Core;
using Union.Playwright.TestSession;
using Union.Playwright.Tests.Fakes;

namespace Union.Playwright.Tests;

[TestFixture]
public class TestSessionProviderTests
{
    private TestableTestSessionProvider _provider = null!;
    private IBrowserContext _fakeContext = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeContext = Substitute.For<IBrowserContext>();
        _provider = new TestableTestSessionProvider();
    }

    #region GetTestSession Tests

    [Test]
    public void GetTestSession_WhenCalledWithNewTest_CreatesNewSession()
    {
        // Arrange
        var browserTest = new FakeBrowserTest(_fakeContext);

        // Act
        var session = _provider.GetTestSession(browserTest);

        // Assert
        session.Should().NotBeNull();
        session.Should().BeOfType<FakeTestSession>();
    }

    [Test]
    public void GetTestSession_WhenCalledTwiceWithSameTest_ReturnsSameSession()
    {
        // Arrange
        var browserTest = new FakeBrowserTest(_fakeContext);

        // Act
        var session1 = _provider.GetTestSession(browserTest);
        var session2 = _provider.GetTestSession(browserTest);

        // Assert
        session1.Should().BeSameAs(session2, "the same test should always return the same session");
    }

    [Test]
    public void GetTestSession_WhenCalledWithDifferentTests_ReturnsDifferentSessions()
    {
        // Arrange
        var browserTest1 = new FakeBrowserTest(_fakeContext);
        var browserTest2 = new FakeBrowserTest(_fakeContext);

        // Act
        var session1 = _provider.GetTestSession(browserTest1);
        var session2 = _provider.GetTestSession(browserTest2);

        // Assert
        session1.Should().NotBeSameAs(session2, "different tests should get different sessions");
    }

    [Test]
    public void GetTestSession_SessionContainsRegisteredServices()
    {
        // Arrange
        var browserTest = new FakeBrowserTest(_fakeContext);

        // Act
        var session = _provider.GetTestSession(browserTest);
        var services = session.GetServices();

        // Assert
        services.Should().NotBeNull();
        services.Should().HaveCountGreaterThan(0, "session should contain at least one service");
    }

    #endregion

    #region Session Lifecycle Tests

    [Test]
    public void GetTestSession_MultipleTests_EachGetsIsolatedSession()
    {
        // Arrange
        var tests = Enumerable.Range(0, 5)
            .Select(_ => new FakeBrowserTest(_fakeContext))
            .ToList();

        // Act
        var sessions = tests.Select(t => _provider.GetTestSession(t)).ToList();

        // Assert
        sessions.Should().OnlyHaveUniqueItems("each test should have its own unique session");
    }

    [Test]
    public void GetTestSession_SameTestMultipleTimes_AlwaysReturnsSameInstance()
    {
        // Arrange
        var browserTest = new FakeBrowserTest(_fakeContext);

        // Act
        var sessions = Enumerable.Range(0, 10)
            .Select(_ => _provider.GetTestSession(browserTest))
            .ToList();

        // Assert
        sessions.Distinct().Should().HaveCount(1, "all calls with same test should return identical session");
    }

    #endregion
}
