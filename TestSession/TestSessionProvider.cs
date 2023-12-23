using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Collections.Generic;
using System.Threading.Tasks;
using Union.Playwright.Services;

namespace Union.Playwright.Core
{
    public class TestSettings
    {

    }
    public abstract class TestSessionProvider<T> where T : TestSession.TestSession
    {
        private readonly Dictionary<BrowserTest, ITestSession> TestSessions;
        private readonly IHost TestApp;

        private TestSessionProvider()
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
            TestSessions = new Dictionary<BrowserTest, ITestSession>();
        }

        private ITestSession NewTestSession(BrowserTest test)
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
            TestSessions[test] = testSession;
            return testSession;
        }

        public TService GetService<TService>(BrowserTest test) where TService : IUnionService
        {
            // TODO: use test session scope
            var scope = TestApp.Services.CreateScope();
            var provider = scope.ServiceProvider;
            return provider.GetService<TService>();
        }

        public ITestSession GetTestSession(BrowserTest test)
        {
            if (TestSessions.ContainsKey(test))
            {
                return TestSessions[test];
            }
            return NewTestSession(test);
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
        private BrowserTest _browserTest;
        private Dictionary<IUnionService, IBrowserContext> _contexts;

        public TestAwareServiceContextsPool()
        {
            _contexts = new Dictionary<IUnionService, IBrowserContext>();
        }

        public async Task<IBrowserContext> GetContext(IUnionService service)
        {
            if (_contexts.ContainsKey(service))
            {
                return _contexts[service];
            }
            var context = await _browserTest.NewContext();
            _contexts[service] = context;
            return context;
        }

        // Managing the browser is responsibility of the BrowserTe
        public void SetTest(BrowserTest browserTest)
        {
            _browserTest = browserTest;
        }
    }
}