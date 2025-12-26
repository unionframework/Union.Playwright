using System;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.TestSession
{
    public class PageNotRegisteredException : Exception
    {
        public PageNotRegisteredException(IUnionPage page)
            : base($"There are not services with registered page of type {page.GetType().Name}")
        {
        }
    }
}