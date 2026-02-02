using System.Threading.Tasks;
using Microsoft.Playwright;
using Union.Playwright.Services;

namespace Union.Playwright.Core
{
    public interface IServiceContextsPool
    {
        Task<IBrowserContext> GetContext(IUnionService service);
    }
}
