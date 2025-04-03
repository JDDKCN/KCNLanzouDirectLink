using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace KCNLanzouDirectLink
{
    internal static class GetUrlHelper
    {
        private static readonly HttpClient client;
        private static readonly HttpClient clientNoRedirect;

        static GetUrlHelper()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            client = new HttpClient(handler);

            var handlerNoRedirect = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false
            };

            clientNoRedirect = new HttpClient(handlerNoRedirect);
        }

        /// <summary>
        /// 获取直链
        /// </summary>
        public static async Task<(DownloadState State, string? Url)> GetFullUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return (DownloadState.UrlNotProvided, null);

            var htmlContent = await GetHtmlContentAsync(url);
            if (htmlContent == null)
                return (DownloadState.HtmlContentNotFound, null);

            var intermediateUrl = ExtractAndCombineLinks(htmlContent);
            if (intermediateUrl == null)
                return (DownloadState.IntermediateUrlNotFound, null);

            var (state, finalUrl) = await GetFinalUrlAsync(intermediateUrl);
            return (state, finalUrl);
        }

        /// <summary>
        /// 获取直链(加密)
        /// </summary>
        public static async Task<(DownloadState State, string? Url)> GetFullUrl(string url, string key)
        {
            if (string.IsNullOrEmpty(url))
                return (DownloadState.UrlNotProvided, null);

            var htmlContent = await GetHtmlContentAsync(url);
            if (htmlContent == null)
                return (DownloadState.HtmlContentNotFound, null);

            var postsign = ExtractPatternValue(htmlContent, @"var\s*vidksek\s*=\s*['""]([^'""]+)['""]");
            if (postsign == null)
                return (DownloadState.PostsignNotFound, null);

            var intermediateUrl = await GetHtmlToPassContentAsync(postsign, key);
            if (intermediateUrl == null)
                return (DownloadState.IntermediateUrlNotFound, null);

            var (state, finalUrl) = await GetFinalUrlAsync(intermediateUrl);
            return (state, finalUrl);
        }

        /// <summary>
        /// 模拟浏览器请求Html
        /// </summary>
        public static async Task<string?> GetHtmlContentAsync(string? url)
        {
            if (url == null)
                return null;

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            SetCommonHeaders(request);

            try
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                var match = Regex.Match(content, @"<div class=""mh""><a href=""([^""]+)""");
                if (match.Success)
                {
                    string tpPath = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(tpPath))
                    {
                        var baseUri = new Uri(url);
                        var tpUrl = new Uri(baseUri, tpPath).ToString();

                        request = new HttpRequestMessage(HttpMethod.Get, tpUrl);
                        SetCommonHeaders(request);
                        response = await client.SendAsync(request);
                        response.EnsureSuccessStatusCode();
                        return await response.Content.ReadAsStringAsync();
                    }
                }

                return content;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 模拟浏览器请求Html(信息获取)
        /// </summary>
        public static async Task<string?> GetHtmlContentToInfoAsync(string? url)
        {
            if (url == null)
                return null;

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            try
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 模拟浏览器请求Html(加密)
        /// </summary>
        public static async Task<string?> GetHtmlToPassContentAsync(string postsign, string key)
        {
            LanzouEncryptedFileInfo? flieConfig = await GetHtmlToPassContentDataAsync(postsign, key);
            if (string.IsNullOrEmpty(flieConfig.Domain) || string.IsNullOrEmpty(flieConfig.Url))
                return null;
            return BuildDownloadUrl(flieConfig.Domain, flieConfig.Url);
        }

        /// <summary>
        /// 模拟浏览器请求Html获取json数据(加密)
        /// </summary>
        public static async Task<LanzouEncryptedFileInfo?> GetHtmlToPassContentDataAsync(string postsign, string key)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://syxz.lanzoue.com/ajaxm.php");
            SetCommonHeaders(request);
            request.Headers.Referrer = new Uri("https://syxz.lanzoue.com");

            var postData = new Dictionary<string, string>
            {
                ["action"] = "downprocess",
                ["sign"] = postsign,
                ["p"] = key
            };
            request.Content = new FormUrlEncodedContent(postData);

            try
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(content);

                var fileInfo = new LanzouEncryptedFileInfo
                {
                    Status = jsonResponse["zt"]?.ToObject<int>() ?? 0,
                    Domain = jsonResponse["dom"]?.ToString(),
                    Url = jsonResponse["url"]?.ToString(),
                    FileName = jsonResponse["inf"]?.ToString()
                };

                return fileInfo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取最终直链
        /// </summary>
        public static async Task<(DownloadState State, string? Url)> GetFinalUrlAsync(string? intermediateUrl)
        {
            if (string.IsNullOrEmpty(intermediateUrl))
                return (DownloadState.IntermediateUrlNotFound, null);

            var request = new HttpRequestMessage(HttpMethod.Get, intermediateUrl);
            SetFinalUrlHeaders(request);

            try
            {
                var response = await clientNoRedirect.SendAsync(request);

                if (response.StatusCode != HttpStatusCode.Found && response.StatusCode != HttpStatusCode.MovedPermanently)
                    return (DownloadState.FinalUrlNotFound, null);

                var location = response.Headers.Location;
                if (location == null)
                    return (DownloadState.FinalUrlNotFound, null);

                if (!location.IsAbsoluteUri)
                {
                    var baseUri = new Uri(intermediateUrl);
                    var newUri = new Uri(baseUri, location);
                    return (DownloadState.Success, newUri.ToString());
                }

                return (DownloadState.Success, location.ToString());
            }
            catch
            {
                return (DownloadState.Error, null);
            }
        }

        /// <summary>
        /// 获取中间链接
        /// </summary>
        public static string? ExtractAndCombineLinks(string? htmlContent)
        {
            if (htmlContent == null)
                return null;

            var vkjxld = ExtractPatternValue(htmlContent, @"var\s*vkjxld\s*=\s*['""]([^'""]+)['""]");
            var hyggid = ExtractPatternValue(htmlContent, @"var\s*hyggid\s*=\s*['""]([^'""]+)['""]");

            if (vkjxld == null || hyggid == null)
                return null;

            return vkjxld + hyggid;
        }

        /// <summary>
        /// 正则提取html值
        /// </summary>
        public static string? ExtractPatternValue(string htmlContent, string pattern)
        {
            var regex = new Regex(pattern);
            var match = regex.Match(htmlContent);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// 格式化加密Url
        /// </summary>
        public static string BuildDownloadUrl(string domain, string url)
        {
            return $"{domain}/file/{WebUtility.UrlDecode(url)}";
        }

        /// <summary>
        /// 设置常用的请求头
        /// </summary>
        private static void SetCommonHeaders(HttpRequestMessage request)
        {
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (iPhone; CPU iPhone OS 6_0 like Mac OS X)");
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            request.Headers.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9");
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            request.Headers.Pragma.Add(new NameValueHeaderValue("no-cache"));
            request.Headers.Connection.Add("keep-alive");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
        }

        /// <summary>
        /// 设置获取最终直链的请求头
        /// </summary>
        private static void SetFinalUrlHeaders(HttpRequestMessage request)
        {
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            request.Headers.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "none");
            request.Headers.Add("Sec-Fetch-User", "?1");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
        }
    }
}
