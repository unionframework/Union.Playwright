using Union.Playwright.Core;
using Union.Playwright.Services;

namespace Union.Playwright.Tests.Integration.StackOverflow
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
