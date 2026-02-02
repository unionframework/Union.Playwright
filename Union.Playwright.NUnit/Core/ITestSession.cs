using System.Collections.Generic;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Core
{
    public interface ITestSession
    {
        public List<IUnionService> GetServices();
    }
}