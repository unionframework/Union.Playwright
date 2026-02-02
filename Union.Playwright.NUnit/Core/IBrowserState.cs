using Microsoft.Playwright;
using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Core
{
    public interface IBrowserState
    {
        public IModalWindow? ModalWindow { get; }
        public IUnionPage? Page { get; }
        public void Actualize(IPage page);
        public T? PageAs<T>() where T : class, IUnionPage => Page as T;
        public bool PageIs<T>() where T : IUnionPage;
    }
}