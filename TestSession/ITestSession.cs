using System.Collections.Generic;
using Union.Playwright.Services;

namespace Union.Playwright.Core
{
    public interface ITestSession
    {
        public List<IUnionService> GetServices();
    }
}