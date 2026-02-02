using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Union.Playwright.Services;
using Union.Playwright.TestSession;

namespace Union.Playwright.Core
{
    public class TestSettings
    {

    }
    public abstract class TestSessionProvider<T> where T : class, ITestSession
    {
        private readonly IHost _testApp;

        protected TestSessionProvider()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices((context, services) =>
            {
                services.AddScoped<IWeb, Web>();
                services.AddScoped<ITestSession, T>();
                services.AddScoped<IServiceContextsPool, TestAwareServiceContextsPool>();
                var settings = context.Configuration.GetSection("TestSettings").Get<TestSettings>();
                if(settings != null)
                {
                    services.AddSingleton(settings);
                }
                ConfigureServices(services);
            });
            this._testApp = builder.Build();
        }

        public ScopedTestSession CreateTestSession(Func<IPage> pageFactory)
        {
            var scope = this._testApp.Services.CreateScope();
            var provider = scope.ServiceProvider;

            var pool = provider.GetRequiredService<IServiceContextsPool>();
            if (pool is TestAwareServiceContextsPool testPool)
            {
                testPool.SetPageFactory(pageFactory);
            }

            var session = provider.GetRequiredService<ITestSession>();
            var web = provider.GetRequiredService<IWeb>();
            session.GetServices().ForEach(s => web.RegisterService(s));

            return new ScopedTestSession(session, scope);
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

    public class TestAwareServiceContextsPool : IServiceContextsPool, IDisposable
    {
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

                var factory = this._pageFactory
                    ?? throw new InvalidOperationException(
                        "No page factory has been configured. " +
                        "Call SetPageFactory() before requesting a context.");
                var page = factory();
                var context = page.Context;

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

        public void Dispose()
        {
            this._contexts.Clear();
            this._lock.Dispose();
        }
    }
}
