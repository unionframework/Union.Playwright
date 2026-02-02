using Microsoft.Extensions.DependencyInjection;
using Union.Playwright.Core;

namespace Union.Playwright.TestSession
{
    public class ScopedTestSession : IDisposable
    {
        public ITestSession Session { get; }
        private readonly IServiceScope _scope;

        public ScopedTestSession(ITestSession session, IServiceScope scope)
        {
            this.Session = session;
            this._scope = scope;
        }

        public void Dispose() => this._scope.Dispose();
    }
}
