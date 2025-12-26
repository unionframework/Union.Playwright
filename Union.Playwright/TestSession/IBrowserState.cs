using Microsoft.Playwright;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Core
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