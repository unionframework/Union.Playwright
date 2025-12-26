using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Routing
{
    // TODO: router should be a singleton
    public class MatchUrlRouter : IRouter
    {
        private readonly Dictionary<Type, IUnionPage> _pages;


        public MatchUrlRouter()
        {
            _pages = new Dictionary<Type, IUnionPage>();
        }


        public RequestData GetRequest(IUnionPage page, BaseUrlInfo defaultBaseUrlInfo)
        {
            return page.GetRequest(defaultBaseUrlInfo);
        }

        public IUnionPage GetPage(RequestData requestData, BaseUrlInfo baseUrlInfo)
        {
            foreach (var dummyPage in _pages.Values)
            {
                var matcher = new UriMatcher(dummyPage.AbsolutePath, dummyPage.Data, dummyPage.Params);
                var match = matcher.Match(requestData.Url, baseUrlInfo.AbsolutePath);
                if (match.Success)
                {
                    var instance = (IUnionPage)Activator.CreateInstance(dummyPage.GetType());
                    instance.BaseUrlInfo = baseUrlInfo;
                    instance.Data = match.Data;
                    instance.Params = match.Params;
                    instance.Cookies = match.Cookies;
                    return instance;
                }
            }
            return null;
        }

        public List<Type> GetPageTypes() => _pages.Keys.ToList();

        public bool HasPage(IUnionPage page) => _pages.ContainsKey(page.GetType());

        //        public void RegisterDerivedPages<T>() where T : SelfMatchingPageBase {
        //            Type superType = typeof (T);
        //            Assembly assembly = superType.GetTypeInfo().Assembly;
        //            IEnumerable<Type> derivedTypes =
        //                assembly.DefinedTypes.AsEnumerable().Where(t => !t.GetTypeInfo().IsAbstract && superType.IsAssignableFrom(t));
        //            foreach (Type derivedType in derivedTypes)
        //                RegisterPage(derivedType);
        //        }

        public void RegisterPage<T>()
        {
            RegisterPage(typeof(T));
        }

        public void RegisterPage(Type pageType)
        {
            var pageInstance = (IUnionPage)Activator.CreateInstance(pageType);
            _pages.Add(pageType, pageInstance);
        }

        public void RegisterDerivedPages<T>()
        {
            var basePageType = typeof(T);
            var derivedPageTypes = Assembly.GetAssembly(basePageType)
                .GetTypes()
                .Where(t => !t.IsAbstract && basePageType.IsAssignableFrom(t));
            foreach (var derivedPageType in derivedPageTypes)
            {
                RegisterPage(derivedPageType);
            }
        }
    }


}