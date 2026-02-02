using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using Union.Playwright.Core;
using Union.Playwright.Services;

namespace Union.Playwright.TestSession
{
    public abstract class UnionTest<TSession> : PageTest where TSession : class, ITestSession
    {
        private ScopedTestSession? _scopedSession;

        protected TSession Session { get; private set; } = null!;

        protected abstract TestSessionProvider<TSession> GetSessionProvider();

        [SetUp]
        public void UnionSetUp()
        {
            _scopedSession = this.GetSessionProvider().CreateTestSession(() => this.Page);
            this.Session = (TSession)_scopedSession.Session;
        }

        [TearDown]
        public void UnionTearDown()
        {
            _scopedSession?.Dispose();
            _scopedSession = null;
        }

        protected TService GetService<TService>() where TService : IUnionService
        {
            return this.Session.GetServices().OfType<TService>().First();
        }
    }
}
