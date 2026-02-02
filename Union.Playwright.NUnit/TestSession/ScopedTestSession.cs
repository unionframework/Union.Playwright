using Microsoft.Extensions.DependencyInjection;
using Union.Playwright.NUnit.Core;

namespace Union.Playwright.NUnit.TestSession
{
    public class ScopedTestSession : IDisposable
    {
        public ITestSession Session { get; }
        private readonly IServiceScope _scope;

        public ScopedTestSession(ITestSession session, IServiceScope scope)
        {
            this.Session = session;
            _scope = scope;
        }

        public void Dispose() => _scope.Dispose();
    }
}
