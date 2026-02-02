using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.Core;
using Union.Playwright.Services;

namespace Union.Playwright.Tests;

/// <summary>
/// Unit tests for TestAwareServiceContextsPool.
/// </summary>
[TestFixture]
public class TestAwareServiceContextsPoolTests
{
    private TestAwareServiceContextsPool _pool = null!;
    private IPage _fakePage = null!;
    private IBrowserContext _fakeContext = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeContext = Substitute.For<IBrowserContext>();
        _fakePage = Substitute.For<IPage>();
        _fakePage.Context.Returns(_fakeContext);
        _pool = new TestAwareServiceContextsPool();
    }

    [TearDown]
    public void TearDown()
    {
        _pool.Dispose();
    }

    #region SetPageFactory Tests

    [Test]
    public void SetPageFactory_DoesNotThrow()
    {
        // Act
        var act = () => _pool.SetPageFactory(() => _fakePage);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region GetContext Tests

    [Test]
    public async Task GetContext_WhenPageFactorySet_ReturnsContext()
    {
        // Arrange
        _pool.SetPageFactory(() => _fakePage);
        var service = Substitute.For<IUnionService>();

        // Act
        var context = await _pool.GetContext(service);

        // Assert
        context.Should().NotBeNull();
        context.Should().BeSameAs(_fakeContext);
    }

    [Test]
    public async Task GetContext_WhenCalledTwiceWithSameService_ReturnsSameContext()
    {
        // Arrange
        _pool.SetPageFactory(() => _fakePage);
        var service = Substitute.For<IUnionService>();

        // Act
        var context1 = await _pool.GetContext(service);
        var context2 = await _pool.GetContext(service);

        // Assert
        context1.Should().BeSameAs(context2, "same service should return same context");
    }

    [Test]
    public void GetContext_WhenNoPageFactorySet_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = Substitute.For<IUnionService>();

        // Act
        Func<Task> act = async () => await _pool.GetContext(service);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*page factory*");
    }

    #endregion

    #region Context Caching Tests

    [Test]
    public async Task GetContext_CachesContextPerService()
    {
        // Arrange
        var callCount = 0;
        var mockPage = Substitute.For<IPage>();
        mockPage.Context.Returns(_ =>
        {
            callCount++;
            return _fakeContext;
        });
        _pool.SetPageFactory(() => mockPage);
        var service = Substitute.For<IUnionService>();

        // Act - call multiple times with same service
        await _pool.GetContext(service);
        await _pool.GetContext(service);
        await _pool.GetContext(service);

        // Assert - factory called once (first GetContext), then cached
        callCount.Should().Be(1, "context should be cached after first call");
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_DoesNotThrow()
    {
        // Act
        var act = () => _pool.Dispose();

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}
