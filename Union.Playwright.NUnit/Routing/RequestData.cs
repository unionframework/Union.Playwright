using System.Collections.Generic;
using System;
using Microsoft.Playwright;

namespace Union.Playwright.NUnit.Routing
{
    public class RequestData
    {
        public RequestData(string url)
            : this(url, new List<Cookie>())
        {
        }

        public RequestData(string url, List<Cookie> cookies)
        {
            Url = new Uri(url);
            Cookies = cookies;
        }

        public Uri Url { get; }

        public List<Cookie> Cookies { get; }
    }
}