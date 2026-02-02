using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Services;
using Union.Playwright.NUnit.TestSession;
using Union.Playwright.NUnit.Tests.Fakes;

namespace Union.Playwright.NUnit.Tests.TestSession;

/// <summary>
/// Tests that verify thread-safety behavior during parallel test execution.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
[Category("Concurrency")]
[Category("ThreadSafety")]
public class ConcurrencyTests
{
    private IPage CreateFakePage()
    {
        var mockContext = Substitute.For<IBrowserContext>();
        var mockPage = Substitute.For<IPage>();
        mockPage.Context.Returns(mockContext);
        return mockPage;
    }

    #region TestSessionProvider Concurrency Tests

    [Test]
    [Repeat(10)]
    [Description("Verifies CreateTestSession doesn't throw when called from multiple threads")]
    public async Task CreateTestSession_WhenCalledConcurrently_DoesNotThrow()
    {
        // Arrange
        var provider = new TestableTestSessionProvider();
        var exceptions = new ConcurrentBag<Exception>();
        var scopedSessions = new ConcurrentBag<ScopedTestSession>();

        // Act - call CreateTestSession concurrently from multiple threads
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() =>
        {
            try
            {
                var scoped = provider.CreateTestSession(() => CreateFakePage());
                scopedSessions.Add(scoped);
                return scoped;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                return null;
            }
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty("concurrent access should not throw exceptions");
        results.Should().AllSatisfy(r => r.Should().NotBeNull());

        // Cleanup
        foreach (var s in scopedSessions) s?.Dispose();
    }

    [Test]
    [Repeat(10)]
    [Description("Verifies concurrent CreateTestSession calls produce distinct sessions")]
    public async Task CreateTestSession_WhenCalledConcurrently_ProducesDistinctSessions()
    {
        // Arrange
        var provider = new TestableTestSessionProvider();
        var scopedSessions = new ConcurrentBag<ScopedTestSession>();

        // Act
        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            var scoped = provider.CreateTestSession(() => CreateFakePage());
            scopedSessions.Add(scoped);
            return scoped;
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        var sessions = results.Select(r => r!.Session).ToList();
        sessions.Should().OnlyHaveUniqueItems("each call should create a distinct session");

        // Cleanup
        foreach (var s in scopedSessions) s?.Dispose();
    }

    #endregion

    #region TestAwareServiceContextsPool Concurrency Tests

    [Test]
    [Repeat(10)]
    [Description("Verifies GetContext with page factory doesn't throw when called from multiple threads")]
    public async Task GetContext_WithPageFactory_WhenCalledConcurrently_DoesNotThrow()
    {
        // Arrange
        var pool = new TestAwareServiceContextsPool();
        var mockContext = Substitute.For<IBrowserContext>();
        var mockPage = Substitute.For<IPage>();
        mockPage.Context.Returns(mockContext);
        pool.SetPageFactory(() => mockPage);

        var services = Enumerable.Range(0, 50)
            .Select(_ => Substitute.For<IUnionService>())
            .ToList();
        var exceptions = new ConcurrentBag<Exception>();

        // Act
        var tasks = services.Select(async service =>
        {
            try
            {
                return await pool.GetContext(service);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty("concurrent context access should not throw");

        // Cleanup
        pool.Dispose();
    }

    [Test]
    [Repeat(10)]
    [Description("Same service requested concurrently should return same context")]
    public async Task GetContext_WithPageFactory_SameServiceConcurrently_ReturnsSameContext()
    {
        // Arrange
        var pool = new TestAwareServiceContextsPool();
        var mockContext = Substitute.For<IBrowserContext>();
        var mockPage = Substitute.For<IPage>();
        mockPage.Context.Returns(mockContext);
        pool.SetPageFactory(() => mockPage);

        var sharedService = Substitute.For<IUnionService>();
        var threadCount = 20;
        var barrier = new Barrier(threadCount);

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await pool.GetContext(sharedService);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Distinct().Should().HaveCount(1,
            "all concurrent calls should return the same context for the same service");

        // Cleanup
        pool.Dispose();
    }

    #endregion
}
