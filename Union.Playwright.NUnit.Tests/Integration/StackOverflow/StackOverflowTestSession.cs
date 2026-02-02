using Union.Playwright.NUnit.Core;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Tests.Integration.StackOverflow
{
    public class StackOverflowTestSession : ITestSession
    {
        private readonly StackOverflowService _soService;

        public StackOverflowTestSession(StackOverflowService soService)
        {
            _soService = soService;
        }

        public List<IUnionService> GetServices() => new() { _soService };

        public StackOverflowService SO => _soService;
    }
}
