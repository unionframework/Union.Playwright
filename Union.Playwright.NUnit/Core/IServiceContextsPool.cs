using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.NUnit.Services;

namespace Union.Playwright.NUnit.Core
{
    public interface IServiceContextsPool
    {
        Task<IBrowserContext> GetContext(IUnionService service);
    }
}
