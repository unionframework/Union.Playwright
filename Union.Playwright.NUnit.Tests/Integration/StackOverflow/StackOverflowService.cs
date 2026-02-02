using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
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
