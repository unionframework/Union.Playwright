using System.Collections.Generic;
using System;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Routing
{
    public interface IRouter
    {
        RequestData GetRequest(IUnionPage page, BaseUrlInfo defaultBaseUrlInfo);
        IUnionPage GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo);
        List<Type> GetPageTypes();

        bool HasPage(IUnionPage page);
    }
}