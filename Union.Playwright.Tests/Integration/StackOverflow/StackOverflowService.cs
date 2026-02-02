using Union.Playwright.Core;
using Union.Playwright.Services;

namespace Union.Playwright.Tests.Integration.StackOverflow
{
    public class StackOverflowService : UnionService<StackOverflowPage>
    {
        public StackOverflowService(IServiceContextsPool serviceContextsPool)
            : base(serviceContextsPool)
        {
        }

        public override string BaseUrl => "https://stackoverflow.com";
    }
}
