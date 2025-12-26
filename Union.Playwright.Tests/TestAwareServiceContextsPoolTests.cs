using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.Core;
using Union.Playwright.Services;
using Union.Playwright.Tests.Fakes;

namespace Union.Playwright.Tests;

/// <summary>
/// Unit tests for TestAwareServiceContextsPool.
///
/// NOTE: Some tests that require intercepting BrowserTest.NewContext() are skipped
/// because BrowserTest.NewContext() is not virtual, preventing proper mocking.
/// The context caching behavior is instead verified through the ConcurrencyTests.
/// </summary>
[TestFixture]
public class TestAwareServiceContextsPoolTests
{
    private TestAwareServiceContextsPool _pool = null!;
    private IBrowserContext _fakeContext = null!;
    private FakeBrowserTest _fakeBrowserTest = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeContext = Substitute.For<IBrowserContext>();
        _fakeBrowserTest = new FakeBrowserTest(_fakeContext);
        _pool = new TestAwareServiceContextsPool();
    }

    #region SetTest Tests

    [Test]
    public void SetTest_DoesNotThrow()
    {
        // Act
        var act = () => _pool.SetTest(_fakeBrowserTest);

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void SetTest_CanBeCalledMultipleTimes()
    {
        // Arrange
        var anotherBrowserTest = new FakeBrowserTest(_fakeContext);

        // Act & Assert - no exception
        _pool.SetTest(_fakeBrowserTest);
        _pool.SetTest(anotherBrowserTest);
    }

    #endregion

    #region GetContext Tests

    [Test]
    //[Ignore("Cannot test: BrowserTest.NewContext() is not virtual, so FakeBrowserTest.NewContext() is not called")]
    public async Task GetContext_WhenSetTestCalled_ReturnsContext()
    {
        // This test cannot work because TestAwareServiceContextsPool calls
        // _browserTest.NewContext() which calls the BASE class method
        // (BrowserTest.NewContext), not our FakeBrowserTest.NewContext().

        // Arrange
        _pool.SetTest(_fakeBrowserTest);
        var service = Substitute.For<IUnionService>();

        // Act
        var context = await _pool.GetContext(service);

        // Assert
        context.Should().NotBeNull();
        context.Should().BeSameAs(_fakeContext);
    }

    [Test]
    [Ignore("Cannot test: BrowserTest.NewContext() is not virtual")]
    public async Task GetContext_WhenCalledTwiceWithSameService_ReturnsSameContext()
    {
        // Arrange
        _pool.SetTest(_fakeBrowserTest);
        var service = Substitute.For<IUnionService>();

        // Act
        var context1 = await _pool.GetContext(service);
        var context2 = await _pool.GetContext(service);

        // Assert
        context1.Should().BeSameAs(context2, "same service should return same context");
    }

    [Test]
    [Ignore("Cannot test: BrowserTest.NewContext() is not virtual")]
    public async Task GetContext_WhenCalledWithDifferentServices_CreatesSeparateContexts()
    {
        // Arrange
        var contextCount = 0;
        var browserTestWithFactory = new FakeBrowserTest(() =>
        {
            contextCount++;
            return Substitute.For<IBrowserContext>();
        });
        _pool.SetTest(browserTestWithFactory);

        var service1 = Substitute.For<IUnionService>();
        var service2 = Substitute.For<IUnionService>();

        // Act
        await _pool.GetContext(service1);
        await _pool.GetContext(service2);

        // Assert
        contextCount.Should().Be(2, "each service should get its own context");
    }

    [Test]
    public void GetContext_WhenSetTestNotCalled_ThrowsNullReferenceException()
    {
        // Arrange
        var service = Substitute.For<IUnionService>();
        // SetTest NOT called - _browserTest is null

        // Act
        Func<Task> act = async () => await _pool.GetContext(service);

        // Assert
        act.Should().ThrowAsync<NullReferenceException>();
    }

    #endregion

    #region Context Caching Tests

    [Test]
    [Ignore("Cannot test: BrowserTest.NewContext() is not virtual")]
    public async Task GetContext_CachesContextPerService()
    {
        // Arrange
        var callCount = 0;
        var browserTestWithCounter = new FakeBrowserTest(() =>
        {
            callCount++;
            return Substitute.For<IBrowserContext>();
        });
        _pool.SetTest(browserTestWithCounter);
        var service = Substitute.For<IUnionService>();

        // Act - call multiple times with same service
        await _pool.GetContext(service);
        await _pool.GetContext(service);
        await _pool.GetContext(service);

        // Assert
        callCount.Should().Be(1, "context should be cached after first call");
    }

    #endregion
}
