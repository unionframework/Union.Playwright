using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Union.Playwright.Services;

namespace Union.Playwright.Core
{
    public class TestSettings
    {

    }
    public abstract class TestSessionProvider<T> where T : TestSession.TestSession
    {
        private readonly ConcurrentDictionary<BrowserTest, ITestSession> TestSessions;
        private readonly IHost TestApp;

        protected TestSessionProvider()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices((context, services) =>
            {
                services.AddScoped<IWeb, Web>();
                services.AddTransient<ITestSession, T>();
                services.AddTransient<IServiceContextsPool, TestAwareServiceContextsPool>();
                var settings = context.Configuration.GetSection("TestSettings").Get<TestSettings>();
                if(settings != null)
                {
                    services.AddSingleton(settings);
                }
                ConfigureServices(services);
            });
            TestApp = builder.Build();
            TestSessions = new ConcurrentDictionary<BrowserTest, ITestSession>();
        }

        private ITestSession CreateTestSession(BrowserTest test)
        {
            var scope = TestApp.Services.CreateScope();
            var provider = scope.ServiceProvider;
            var testSession = provider.GetService<ITestSession>();
            var contextsPool = provider.GetService<IServiceContextsPool>();
            var web = provider.GetService<IWeb>();
            // TODO: verify for null
            if (contextsPool is TestAwareServiceContextsPool)
            {
                ((TestAwareServiceContextsPool)contextsPool).SetTest(test);
            }
            var services = testSession.GetServices();
            services.ForEach(s => web.RegisterService(s));
            return testSession;
        }

        public TService GetService<TService>(BrowserTest test) where TService : IUnionService
        {
            // TODO: use test session scope
            var scope = TestApp.Services.CreateScope();
            var provider = scope.ServiceProvider;
            return provider.GetService<TService>();
        }

        /// <summary>
        /// Gets or creates a test session for the given browser test.
        /// Thread-safe: uses ConcurrentDictionary.GetOrAdd for atomic access.
        /// </summary>
        public ITestSession GetTestSession(BrowserTest test)
        {
            return TestSessions.GetOrAdd(test, CreateTestSession);
        }

        /// <summary>
        /// Implement this method to configure dependencies
        /// </summary>
        /// <param name="services"></param>
        public abstract void ConfigureServices(IServiceCollection services);
    }

    public interface IServiceContextsPool
    {
        Task<IBrowserContext> GetContext(IUnionService service);
    }

    public class TestAwareServiceContextsPool : IServiceContextsPool
    {
        private BrowserTest? _browserTest;
        private Func<IPage>? _pageFactory;
        private readonly ConcurrentDictionary<IUnionService, IBrowserContext> _contexts;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public TestAwareServiceContextsPool()
        {
            this._contexts = new ConcurrentDictionary<IUnionService, IBrowserContext>();
        }

        /// <summary>
        /// Gets or creates a browser context for the given service.
        /// Thread-safe: uses ConcurrentDictionary with SemaphoreSlim for async-safe access.
        /// </summary>
        public async Task<IBrowserContext> GetContext(IUnionService service)
        {
            // Fast path: check if context already exists
            if (this._contexts.TryGetValue(service, out var existingContext))
            {
                return existingContext;
            }

            // Slow path: acquire lock and create context if needed
            await this._lock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (this._contexts.TryGetValue(service, out existingContext))
                {
                    return existingContext;
                }

                IBrowserContext context;
                if (this._pageFactory != null)
                {
                    var page = this._pageFactory();
                    context = page.Context;
                }
                else if (this._browserTest != null)
                {
                    context = await this._browserTest.NewContext();
                }
                else
                {
                    throw new InvalidOperationException(
                        "Neither a page factory nor a BrowserTest has been configured. " +
                        "Call SetPageFactory() or SetTest() before requesting a context.");
                }

                this._contexts[service] = context;
                return context;
            }
            finally
            {
                this._lock.Release();
            }
        }

        /// <summary>
        /// Sets a factory function that provides the current IPage instance.
        /// Used by UnionTest to integrate with PageTest's page lifecycle.
        /// </summary>
        public void SetPageFactory(Func<IPage> pageFactory)
        {
            this._pageFactory = pageFactory;
        }

        /// <summary>
        /// Sets the BrowserTest instance for context creation.
        /// Legacy method - prefer SetPageFactory for new code.
        /// </summary>
        public void SetTest(BrowserTest browserTest)
        {
            this._browserTest = browserTest;
        }
    }
}