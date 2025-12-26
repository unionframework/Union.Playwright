using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Playwright;
using NSubstitute;
using NUnit.Framework;
using Union.Playwright.Core;
using Union.Playwright.Services;
using Union.Playwright.Tests.Fakes;

namespace Union.Playwright.Tests.TestSession;

/// <summary>
/// Tests that verify thread-safety behavior during parallel test execution.
/// Expected behavior:
/// - Tests use [Repeat(10)] to increase the chance of detecting race conditions
/// - Barrier class is used to synchronize threads and maximize concurrency
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
[Category("Concurrency")]
[Category("ThreadSafety")]
public class ConcurrencyTests
{
    #region TestSessionProvider Concurrency Tests

    [Test]
    [Repeat(10)]
    [Description("Verifies GetTestSession doesn't throw when called from multiple threads")]
    public async Task GetTestSession_WhenCalledConcurrently_DoesNotThrow()
    {
        // Arrange
        var provider = new TestableTestSessionProvider();
        var exceptions = new ConcurrentBag<Exception>();

        // Create multiple fake browser tests
        var browserTests = Enumerable.Range(0, 50)
            .Select(_ => new FakeBrowserTest(Substitute.For<IBrowserContext>()))
            .ToList();

        // Act - call GetTestSession concurrently from multiple threads
        var tasks = browserTests.Select(async bt =>
        {
            try
            {
                return await Task.Run(() => provider.GetTestSession(bt));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty("concurrent access should not throw exceptions");
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    [Test]
    [Repeat(10)]
    [Description("Documents race condition: multiple threads requesting session for SAME test should return identical session")]
    public async Task GetTestSession_WhenCalledConcurrentlyWithSameTest_ReturnsSameSession()
    {
        // Arrange
        var provider = new TestableTestSessionProvider();
        var fakeContext = Substitute.For<IBrowserContext>();
        var sharedBrowserTest = new FakeBrowserTest(fakeContext);
        var sessions = new ConcurrentBag<ITestSession>();
        var barrier = new Barrier(20); // Synchronize 20 threads

        // Act - multiple threads requesting session for SAME test
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() =>
            {
                barrier.SignalAndWait(); // All threads start at same time
                var session = provider.GetTestSession(sharedBrowserTest);
                sessions.Add(session);
                return session;
            }));

        var results = await Task.WhenAll(tasks);

        // Assert - all should return the same session instance
        // NOTE: This test may fail due to race condition in GetTestSession
        // where ContainsKey + NewTestSession is not atomic
        sessions.Should().HaveCount(20);
        results.Distinct().Should().HaveCount(1,
            "all concurrent calls should return the same session for the same test. " +
            "If this fails, it indicates a race condition in TestSessionProvider.GetTestSession()");
    }

    [Test]
    [Repeat(10)]
    [Description("Documents race condition: verifies only one session is created per test even under high concurrency")]
    public async Task GetTestSession_RaceConditionScenario_OnlyCreatesOneSessionPerTest()
    {
        // This test specifically targets the race condition in:
        // if (TestSessions.ContainsKey(test)) { return TestSessions[test]; }
        // return NewTestSession(test);

        // Arrange
        var provider = new TestableTestSessionProvider();
        var fakeContext = Substitute.For<IBrowserContext>();
        var browserTest = new FakeBrowserTest(fakeContext);
        var createdSessions = new ConcurrentBag<ITestSession>();
        var threadCount = 10;
        var barrier = new Barrier(threadCount);

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
        {
            barrier.SignalAndWait(); // All threads hit GetTestSession at same time
            var session = provider.GetTestSession(browserTest);
            createdSessions.Add(session);
            return session;
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Distinct().Should().HaveCount(1,
            "only one session should be created even under race condition. " +
            "Multiple distinct sessions indicate thread-safety bug.");
    }

    [Test]
    [Description("Stress test: verifies behavior under sustained concurrent access")]
    public async Task GetTestSession_StressTest_HandlesManyConcurrentRequests()
    {
        // Arrange
        var provider = new TestableTestSessionProvider();
        var exceptions = new ConcurrentBag<Exception>();
        var requestCount = 100;

        // Create a pool of browser tests
        var browserTests = Enumerable.Range(0, 10)
            .Select(_ => new FakeBrowserTest(Substitute.For<IBrowserContext>()))
            .ToArray();

        // Act - many concurrent requests using random tests from pool
        var random = new Random(42); // Fixed seed for reproducibility
        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    var testIndex = random.Next(browserTests.Length);
                    return provider.GetTestSession(browserTests[testIndex]);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    return null;
                }
            }));

        var results = await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty("stress test should complete without exceptions");
    }

    #endregion

    #region TestAwareServiceContextsPool Concurrency Tests

    // NOTE: These tests are ignored because BrowserTest.NewContext() is not virtual,
    // so FakeBrowserTest cannot properly intercept the call. The TestAwareServiceContextsPool
    // calls _browserTest.NewContext() which invokes the base BrowserTest method
    // (which requires actual Playwright browser infrastructure).
    //
    // The race condition in TestAwareServiceContextsPool.GetContext() is documented
    // in the code comments and follows the same pattern as TestSessionProvider.GetTestSession().

    [Test]
    [Ignore("Cannot test: BrowserTest.NewContext() is not virtual - requires actual Playwright browser")]
    [Repeat(10)]
    [Description("Verifies GetContext doesn't throw when called from multiple threads")]
    public async Task GetContext_WhenCalledConcurrently_DoesNotThrow()
    {
        // Arrange
        var pool = new TestAwareServiceContextsPool();
        var fakeContext = Substitute.For<IBrowserContext>();
        pool.SetTest(new FakeBrowserTest(fakeContext));

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
    }

    [Test]
    [Ignore("Cannot test: BrowserTest.NewContext() is not virtual - requires actual Playwright browser")]
    [Repeat(10)]
    [Description("Documents race condition: same service requested concurrently should return same context")]
    public async Task GetContext_WhenCalledConcurrentlyWithSameService_ReturnsSameContext()
    {
        // Arrange
        var pool = new TestAwareServiceContextsPool();
        var fakeContext = Substitute.For<IBrowserContext>();
        pool.SetTest(new FakeBrowserTest(fakeContext));

        var sharedService = Substitute.For<IUnionService>();
        var contexts = new ConcurrentBag<IBrowserContext>();
        var threadCount = 20;
        var barrier = new Barrier(threadCount);

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            var context = await pool.GetContext(sharedService);
            contexts.Add(context);
            return context;
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        contexts.Should().HaveCount(threadCount);
        results.Distinct().Should().HaveCount(1,
            "all concurrent calls should return the same context for the same service. " +
            "If this fails, it indicates a race condition in TestAwareServiceContextsPool.GetContext()");
    }

    [Test]
    [Ignore("Cannot test: BrowserTest.NewContext() is not virtual - requires actual Playwright browser")]
    [Repeat(10)]
    [Description("Documents race condition: verifies NewContext is only called once per service")]
    public async Task GetContext_RaceConditionScenario_OnlyCreatesOneContextPerService()
    {
        // Tests the race condition in:
        // if (_contexts.ContainsKey(service)) { return _contexts[service]; }
        // var context = await _browserTest.NewContext();
        // _contexts[service] = context;

        // Arrange
        var pool = new TestAwareServiceContextsPool();
        var contextCreationCount = 0;
        var fakeBrowserTest = new FakeBrowserTest(() =>
        {
            Interlocked.Increment(ref contextCreationCount);
            return Substitute.For<IBrowserContext>();
        });
        pool.SetTest(fakeBrowserTest);

        var service = Substitute.For<IUnionService>();
        var threadCount = 10;
        var barrier = new Barrier(threadCount);

        // Act
        var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await pool.GetContext(service);
        }));

        var results = await Task.WhenAll(tasks);

        // Assert
        // With current (non-thread-safe) implementation, contextCreationCount may be > 1
        // After fix, it should be exactly 1
        contextCreationCount.Should().Be(1,
            "only one context should be created per service. " +
            "If contextCreationCount > 1, it indicates the ContainsKey check is not atomic.");

        results.Distinct().Should().HaveCount(1,
            "all threads should receive the same context instance");
    }

    [Test]
    [Ignore("Cannot test: BrowserTest.NewContext() is not virtual - requires actual Playwright browser")]
    [Description("Stress test: verifies GetContext behavior under sustained concurrent access")]
    public async Task GetContext_StressTest_HandlesManyConcurrentRequests()
    {
        // Arrange
        var pool = new TestAwareServiceContextsPool();
        pool.SetTest(new FakeBrowserTest(() => Substitute.For<IBrowserContext>()));

        var exceptions = new ConcurrentBag<Exception>();
        var requestCount = 100;

        // Create a pool of services
        var services = Enumerable.Range(0, 10)
            .Select(_ => Substitute.For<IUnionService>())
            .ToArray();

        // Act - many concurrent requests using random services from pool
        var random = new Random(42);
        var tasks = Enumerable.Range(0, requestCount)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var serviceIndex = random.Next(services.Length);
                    return await pool.GetContext(services[serviceIndex]);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                    return null;
                }
            }));

        var results = await Task.WhenAll(tasks);

        // Assert
        exceptions.Should().BeEmpty("stress test should complete without exceptions");
    }

    #endregion
}
