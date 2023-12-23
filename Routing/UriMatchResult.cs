using Microsoft.Playwright;
using System.Collections.Generic;

namespace Union.Playwright.Routing
{
    public class UriMatchResult
    {
        public UriMatchResult(bool success)
            : this(success, new Dictionary<string, string>())
        {
        }

        public UriMatchResult(bool success, Dictionary<string, string> data)
            : this(success, data, new Dictionary<string, string>())
        {
        }

        public UriMatchResult(bool success, Dictionary<string, string> data, Dictionary<string, string> _params)
            : this(success, data, _params, new List<Cookie>())
        {
        }

        public UriMatchResult(
            bool success,
            Dictionary<string, string> data,
            Dictionary<string, string> _params,
            List<Cookie> cookies)
        {
            Success = success;
            Data = data;
            Cookies = cookies;
            Params = _params;
        }

        public bool Success { get; private set; }

        public Dictionary<string, string> Data { get; private set; }

        public List<Cookie> Cookies { get; private set; }

        public Dictionary<string, string> Params { get; set; }

        public static UriMatchResult Matched()
        {
            return new UriMatchResult(true);
        }

        public static UriMatchResult Unmatched()
        {
            return new UriMatchResult(false);
        }
    }


}