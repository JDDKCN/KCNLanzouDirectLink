using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace KCNLanzouDirectLink
{
    internal static class GetUrlHelper
    {
        /// <summary>
        /// 获取直链
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string?> GetFullUrl(string url)
        {
            return await GetFinalUrlAsync(
                    ExtractAndCombineLinks(
                    await GetHtmlContentAsync(url)));
        }

        /// <summary>
        /// 获取直链(加密)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static async Task<string?> GetFullUrl(string url, string key)
        {
            var postsign = ExtractPatternValue(
                await GetHtmlContentAsync(url),
                @"var\s*vidksek\s*=\s*['""]([^'""]+)['""]");
            return await GetFinalUrlToPassAsync(await GetHtmlToPassContentAsync(postsign, key));
        }

        /// <summary>
        /// 模拟浏览器请求Html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string?> GetHtmlContentAsync(string? url)
        {
            if (url == null)
                return null;

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5376e Safari/8536.25");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9");
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Pragma", "no-cache");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

            url = ModifyUrlForTp(url);
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string contentEncoding = response.Content.Headers.ContentEncoding.ToString();

            byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
            return contentEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase)
                ? DecompressGzip(responseBytes)
                : Encoding.UTF8.GetString(responseBytes);
        }

        /// <summary>
        /// 模拟浏览器请求Html(加密)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string?> GetHtmlToPassContentAsync(string postsign, string key)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 6_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5376e Safari/8536.25");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9");
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Pragma", "no-cache");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.Add("Referer", "https://syxz.lanzoue.com");

            var postData = new StringContent($"action=downprocess&sign={postsign}&p={key}", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await client.PostAsync("https://syxz.lanzoue.com/ajaxm.php", postData);

            string content = await response.Content.ReadAsStringAsync();

            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(content);
            string? domain = jsonResponse["dom"]?.ToString();
            string? url = jsonResponse["url"]?.ToString();

            if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(url))
                return null;

            string fullDownloadUrl = BuildDownloadUrl(domain, url);
            return fullDownloadUrl;
        }

        /// <summary>
        /// 获取中间链接
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <returns></returns>
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
        /// 获取最终直链
        /// </summary>
        /// <param name="intermediateUrl"></param>
        /// <returns></returns>
        public static async Task<string?> GetFinalUrlAsync(string? intermediateUrl)
        {
            if (string.IsNullOrEmpty(intermediateUrl))
                return null;

            HttpClientHandler handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
            };

            using HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Host", "slssq.osslan.com:446");
            client.DefaultRequestHeaders.Add("Sec-CH-UA", "\"Chromium\";v=\"130\", \"Microsoft Edge\";v=\"130\", \"Not?A_Brand\";v=\"99\"");
            client.DefaultRequestHeaders.Add("Sec-CH-UA-Mobile", "?0");
            client.DefaultRequestHeaders.Add("Sec-CH-UA-Platform", "\"Windows\"");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0");

            HttpResponseMessage response = await client.GetAsync(intermediateUrl);

            if ((int)response.StatusCode < 300 || (int)response.StatusCode >= 400)
                return null;

            if (!response.Headers.Contains("Location"))
                return null;

            return response.Headers?.Location?.ToString();
        }

        /// <summary>
        /// 获取最终直链(加密)
        /// </summary>
        /// <param name="intermediateUrl"></param>
        /// <returns></returns>
        public static async Task<string?> GetFinalUrlToPassAsync(string? intermediateUrl)
        {
            if (string.IsNullOrEmpty(intermediateUrl))
                return null;

            HttpClientHandler handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
            };

            using HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
            client.DefaultRequestHeaders.Add("Host", "develope-oss.lanzouc.com");
            client.DefaultRequestHeaders.Add("Proxy-Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0");

            HttpResponseMessage response = await client.GetAsync(intermediateUrl);

            if ((int)response.StatusCode < 300 || (int)response.StatusCode >= 400)
                return null;

            if (!response.Headers.Contains("Location"))
                return null;

            return response.Headers?.Location?.ToString();
        }

        /// <summary>
        /// 正则提取html值
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private static string? ExtractPatternValue(string htmlContent, string pattern)
        {
            var regex = new Regex(pattern, RegexOptions.Compiled);
            var match = regex.Match(htmlContent);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// 格式化Url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string ModifyUrlForTp(string url)
        {
            int lastSlashIndex = url.LastIndexOf('/');
            if (lastSlashIndex == -1)
                return url;
            return url.Substring(0, lastSlashIndex + 1) + "tp/" + url.Substring(lastSlashIndex + 1);
        }

        /// <summary>
        /// 格式化加密Url
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string BuildDownloadUrl(string domain, string url)
        {
            string fullUrl = domain + "/file/" + WebUtility.UrlDecode(url);
            return fullUrl;
        }

        /// <summary>
        /// 解压Gzip数据
        /// </summary>
        /// <param name="gzipData"></param>
        /// <returns></returns>
        private static string DecompressGzip(byte[] gzipData)
        {
            using var ms = new MemoryStream(gzipData);
            using var gzipStream = new GZipStream(ms, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
