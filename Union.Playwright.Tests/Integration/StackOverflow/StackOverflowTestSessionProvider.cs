using Microsoft.Extensions.DependencyInjection;
using Union.Playwright.Core;

namespace Union.Playwright.Tests.Integration.StackOverflow
{
    public class StackOverflowTestSessionProvider : TestSessionProvider<StackOverflowTestSession>
    {
        public static readonly StackOverflowTestSessionProvider Instance = new();

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<StackOverflowService>();
        }
    }
}
