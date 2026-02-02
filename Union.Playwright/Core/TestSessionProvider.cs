using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using Union.Playwright.Services;
using Union.Playwright.TestSession;

namespace Union.Playwright.Core
{
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
            _testApp = builder.Build();
        }

        public ScopedTestSession CreateTestSession(Func<IPage> pageFactory)
        {
            var scope = _testApp.Services.CreateScope();
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
}
