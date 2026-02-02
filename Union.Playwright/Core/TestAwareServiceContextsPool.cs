using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.Services;

namespace Union.Playwright.Core
{
    public class TestAwareServiceContextsPool : IServiceContextsPool, IDisposable
    {
        private Func<IPage>? _pageFactory;
        private readonly ConcurrentDictionary<IUnionService, IBrowserContext> _contexts;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public TestAwareServiceContextsPool()
        {
            _contexts = new ConcurrentDictionary<IUnionService, IBrowserContext>();
        }

        /// <summary>
        /// Gets or creates a browser context for the given service.
        /// Thread-safe: uses ConcurrentDictionary with SemaphoreSlim for async-safe access.
        /// </summary>
        public async Task<IBrowserContext> GetContext(IUnionService service)
        {
            // Fast path: check if context already exists
            if (_contexts.TryGetValue(service, out var existingContext))
            {
                return existingContext;
            }

            // Slow path: acquire lock and create context if needed
            await _lock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_contexts.TryGetValue(service, out existingContext))
                {
                    return existingContext;
                }

                var factory = _pageFactory
                    ?? throw new InvalidOperationException(
                        "No page factory has been configured. " +
                        "Call SetPageFactory() before requesting a context.");
                var page = factory();
                var context = page.Context;

                _contexts[service] = context;
                return context;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Sets a factory function that provides the current IPage instance.
        /// Used by UnionTest to integrate with PageTest's page lifecycle.
        /// </summary>
        public void SetPageFactory(Func<IPage> pageFactory)
        {
            _pageFactory = pageFactory;
        }

        public void Dispose()
        {
            _contexts.Clear();
            _lock.Dispose();
        }
    }
}
