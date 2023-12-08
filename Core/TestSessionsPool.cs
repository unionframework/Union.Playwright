using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;

namespace Union.Playwright.Core
{
    public abstract class TestSessionsPool<T> where T: TestSession
    {
        private readonly ThreadLocal<TestSession> TestSessions;
        private readonly IHost TestApp;

        public IServiceProvider Services => TestApp.Services;

        public TestSessionsPool()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddTransient<TestSession, T>();
                ConfigureServices(services);
            });
            TestApp = builder.Build();
            TestSessions = new ThreadLocal<TestSession>(NewTestSession);
        }

        public TestSession NewTestSession() => Services.GetService<TestSession>();

        public TestSession Default => TestSessions.Value;

        /// <summary>
        /// Implement this method to configure dependencies
        /// </summary>
        /// <param name="services"></param>
        public abstract void ConfigureServices(IServiceCollection services);
    }
}
