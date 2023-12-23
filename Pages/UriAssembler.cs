using System.Collections.Generic;
using System.Linq;
using Union.Playwright.Routing;
using Union.Utils.Extensions;

namespace Union.Playwright.Pages
{
    public class UriAssembler
    {
        private readonly string _absolutePath;

        private readonly BaseUrlInfo _baseUrlInfo;

        private readonly Dictionary<string, string> _data;

        private readonly Dictionary<string, string> _params;

        public UriAssembler(
            BaseUrlInfo baseUrlInfo,
            string absolutePath,
            Dictionary<string, string> data,
            Dictionary<string, string> @params)
        {
            _baseUrlInfo = baseUrlInfo;
            _absolutePath = absolutePath;
            _data = data;
            _params = @params;
        }

        public string Assemble(BaseUrlInfo defaultBaseUrlInfo)
        {
            var url = $"http://{GetBaseUrl(defaultBaseUrlInfo)}";

            var path = GetPath();
            if (!string.IsNullOrWhiteSpace(path))
            {
                url = url.CutLast('/') + "/" + GetPath().CutFirst('/');
            }

            var query = GetQuery();
            if (!string.IsNullOrWhiteSpace(query))
            {
                url += "?" + query;
            }

            return url;
        }

        private string GetQuery()
        {
            if (_params == null)
            {
                return string.Empty;
            }
            var query = _params.Keys.Cast<string>()
                .Aggregate(string.Empty, (current, key) => current + key + "=" + _params[key] + "&");
            return query.CutLast('&');
        }

        private string GetPath()
        {
            if (_data == null)
            {
                return _absolutePath;
            }
            var path = _absolutePath;
            foreach (var key in _data.Keys)
            {
                var param = "{" + key + "}";
                path = path.Replace(param, _data[key]);
            }
            return path;
        }

        private string GetBaseUrl(BaseUrlInfo defaultBaseUrlInfo)
        {
            return defaultBaseUrlInfo.ApplyActual(_baseUrlInfo).GetBaseUrl();
        }
    }
}
