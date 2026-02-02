using Union.Playwright.Core;
using Union.Playwright.Services;

namespace Union.Playwright.Tests.Integration.StackOverflow
{
    public class StackOverflowTestSession : ITestSession
    {
        private readonly StackOverflowService _soService;

        public StackOverflowTestSession(StackOverflowService soService)
        {
            this._soService = soService;
        }

        public List<IUnionService> GetServices() => new() { this._soService };

        public StackOverflowService SO => this._soService;
    }
}
