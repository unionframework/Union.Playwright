using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using Union.Playwright.Core;
using Union.Playwright.Services;

namespace Union.Playwright.TestSession
{
    /// <summary>
    /// Generic test base class that derives from Playwright's PageTest,
    /// providing DI support and automatic test session lifecycle management.
    /// </summary>
    /// <typeparam name="TSession">The test session type, must implement ITestSession.</typeparam>
    public abstract class UnionTest<TSession> : PageTest where TSession : class, ITestSession
    {
        private IHost? _host;
        private IServiceScope? _scope;

        /// <summary>
        /// The resolved test session for the current test.
        /// </summary>
        protected TSession Session { get; private set; } = null!;

        /// <summary>
        /// Override to register additional services in the DI container.
        /// </summary>
        protected abstract void ConfigureServices(IServiceCollection services);

        [OneTimeSetUp]
        public void UnionOneTimeSetUp()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddTransient<TSession>();
                services.AddScoped<IServiceContextsPool, TestAwareServiceContextsPool>();
                ConfigureServices(services);
            });
            this._host = builder.Build();
        }

        [SetUp]
        public void UnionSetUp()
        {
            this._scope = this._host!.Services.CreateScope();
            var pool = this._scope.ServiceProvider.GetRequiredService<IServiceContextsPool>();
            if (pool is TestAwareServiceContextsPool testPool)
            {
                testPool.SetPageFactory(() => this.Page);
            }
            this.Session = this._scope.ServiceProvider.GetRequiredService<TSession>();
        }

        [TearDown]
        public void UnionTearDown()
        {
            this._scope?.Dispose();
        }

        [OneTimeTearDown]
        public void UnionOneTimeTearDown()
        {
            this._host?.Dispose();
        }

        /// <summary>
        /// Gets a specific service from the current test session by type.
        /// </summary>
        protected TService GetService<TService>() where TService : IUnionService
        {
            return this.Session.GetServices().OfType<TService>().First();
        }
    }
}
