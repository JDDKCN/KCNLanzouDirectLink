using KCNLanzouDirectLink.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace KCNLanzouDirectLink.Core;

/// <summary>
/// 蓝奏云 HTTP 客户端基类
/// </summary>
internal class LanzouHttpClient
{
    protected readonly HttpClient _client;
    protected readonly HttpClient _clientNoRedirect;
    protected readonly AntiCrawlerHandler _antiCrawlerHandler;
    protected readonly CookieContainer _cookieContainer = new();
    protected LanzouDomainInfo? _domainInfo;

    public LanzouHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = true,
            CookieContainer = _cookieContainer
        };

        _client = new HttpClient(handler);

        var handlerNoRedirect = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = false,
            UseCookies = true,
            CookieContainer = _cookieContainer
        };

        _clientNoRedirect = new HttpClient(handlerNoRedirect);
        _antiCrawlerHandler = new AntiCrawlerHandler();
    }

    /// <summary>
    /// 初始化域名信息
    /// </summary>
    protected bool InitializeDomain(string url)
    {
        _domainInfo = LanzouDomainParser.ParseUrl(url);
        if (_domainInfo == null)
        {
            Debug.WriteLine($"无效的蓝奏云 URL: {url}");
            return false;
        }

        Debug.WriteLine($"解析域名:");
        Debug.WriteLine($"  完整域名: {_domainInfo.FullDomain}");
        Debug.WriteLine($"  基础域名: {_domainInfo.BaseDomain}");
        Debug.WriteLine($"  子域名: {_domainInfo.Subdomain ?? "(无)"}");
        Debug.WriteLine($"  基础 URL: {_domainInfo.BaseUrl}");

        return true;
    }

    /// <summary>
    /// 发送请求
    /// </summary>
    protected async Task<string?> SendRequestWithAntiCrawlerAsync(
        HttpRequestMessage request,
        Dictionary<string, string>? postData = null,
        int maxRetries = 2)
    {
        try
        {
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            // 是否触发反爬虫Cookie验证
            if (_antiCrawlerHandler.IsAntiCrawlerResponse(content))
            {
                Debug.WriteLine($"[{request.RequestUri!.Host}] 检测到反爬虫机制，开始处理...");

                var cookieStr = _antiCrawlerHandler.HandleAntiCrawler(content);
                if (string.IsNullOrEmpty(cookieStr))
                {
                    Debug.WriteLine("反爬虫处理失败");
                    return null;
                }

                AddWafCookie(request.RequestUri!, cookieStr);

                await Task.Delay(500);

                var retryRequest = CloneRequest(request, postData);
                var retryResponse = await _client.SendAsync(retryRequest);
                retryResponse.EnsureSuccessStatusCode();

                content = await retryResponse.Content.ReadAsStringAsync();

                if (_antiCrawlerHandler.IsAntiCrawlerResponse(content))
                {
                    Debug.WriteLine("重试后仍然触发反爬虫");
                    return null;
                }
            }

            // 是否为垃圾广告页面
            int retryCount = 0;
            while (IsTrashAdPage(content) && retryCount < maxRetries)
            {
                retryCount++;
                Debug.WriteLine($"检测到垃圾广告页面，第 {retryCount} 次重试...");

                await Task.Delay(500);

                var retryRequest = CloneRequest(request, postData);
                var retryResponse = await _client.SendAsync(retryRequest);
                retryResponse.EnsureSuccessStatusCode();

                content = await retryResponse.Content.ReadAsStringAsync();
            }

            if (IsTrashAdPage(content))
            {
                Debug.WriteLine($"{maxRetries} 次重试后仍是垃圾页面");
                return null;
            }

            return content;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"请求异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 检测是否是垃圾软件推广页
    /// </summary>
    protected bool IsTrashAdPage(string htmlContent)
    {
        // 包含 "使用全能电脑助手下载"
        if (htmlContent.Contains("使用全能电脑助手下载"))
            return true;

        // 包含 "使用工具进行下载"
        if (htmlContent.Contains("使用工具进行下载"))
            return true;

        // 包含 install.office123456.com
        if (htmlContent.Contains("install.office123456.com"))
            return true;

        // d_pclink 类名
        if (htmlContent.Contains("d_pclink"))
            return true;

        return false;
    }

    /// <summary>
    /// 克隆HTTP请求
    /// </summary>
    private HttpRequestMessage CloneRequest(HttpRequestMessage original, Dictionary<string, string>? postData)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (var header in original.Headers)
        {
            if (header.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase)) 
                continue;

            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (postData != null && original.Method == HttpMethod.Post)
        {
            var rawData = string.Join("&", postData.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            clone.Content = new StringContent(rawData);
            clone.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
        }

        return clone;
    }

    /// <summary>
    /// 设置通用请求头
    /// </summary>
    protected void SetCommonHeaders(HttpRequestMessage request)
    {
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        request.Headers.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.9");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        request.Headers.Pragma.Add(new NameValueHeaderValue("no-cache"));
        request.Headers.Connection.Add("keep-alive");
        request.Headers.Add("Upgrade-Insecure-Requests", "1");
    }

    /// <summary>
    /// 获取最终重定向URL
    /// </summary>
    protected async Task<(bool Success, string? Url)> GetRedirectUrlAsync(string url)
    {
        try
        {
            for (int i = 0; i < 3; i++)
            {
                var uri = new Uri(url);
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);

                SetCommonHeaders(request);
                request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
                request.Headers.TryAddWithoutValidation("X-Requested-With", "mark.via");
                request.Headers.Add("Sec-Fetch-Dest", "document");
                request.Headers.Add("Sec-Fetch-Mode", "navigate");
                request.Headers.Add("Sec-Fetch-Site", "none");
                request.Headers.Add("Sec-Fetch-User", "?1");

                using var response = await _clientNoRedirect.SendAsync(request);

                // 301 / 302，重定向成功
                if (response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.MovedPermanently)
                {
                    var location = response.Headers.Location;
                    if (location == null)
                        return (false, null);

                    var finalUrl = location.IsAbsoluteUri
                        ? location.ToString()
                        : new Uri(new Uri(url), location).ToString();

                    return (true, finalUrl);
                }

                // 200，被拦截
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (_antiCrawlerHandler.IsAntiCrawlerResponse(content))
                    {
                        Debug.WriteLine(
                            $"[{uri.Host}] 获取直链时触发 ESA，计算 acw_sc__v2 (第 {i + 1} 次)...");

                        var cookieStr = _antiCrawlerHandler.HandleAntiCrawler(content);
                        if (!string.IsNullOrEmpty(cookieStr))
                        {
                            AddWafCookie(uri, cookieStr);
                            await Task.Delay(500);
                            continue;
                        }
                    }
                }

                Debug.WriteLine($"重定向失败，最终状态码: {response.StatusCode}");
                break;
            }

            return (false, null);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"获取重定向URL异常: {ex.Message}");
            return (false, null);
        }
    }

    /// <summary>
    /// 将 WAF 验证 Cookie 绑定到当前 Host 以及根域名
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="cookieStr"></param>
    protected void AddWafCookie(Uri uri, string cookieStr)
    {
        var cookieParts = cookieStr.Split(new[] { '=' }, 2);
        if (cookieParts.Length != 2) 
            return;

        var name = cookieParts[0].Trim();
        var val = cookieParts[1].Trim();

        try
        {
            _cookieContainer.Add(new Uri($"{uri.Scheme}://{uri.Host}"), new Cookie(name, val));
        }
        catch { }

        var baseDomain = _domainInfo?.BaseDomain;
        if (string.IsNullOrEmpty(baseDomain))
        {
            var hostParts = uri.Host.Split('.');
            baseDomain = hostParts.Length >= 2
                ? string.Join(".", hostParts.Skip(hostParts.Length - 2))
                : uri.Host;
        }

        try
        {
            _cookieContainer.Add(new Cookie(name, val, "/", "." + baseDomain));
        }
        catch
        {
            try
            {
                _cookieContainer.Add(new Uri($"{uri.Scheme}://{baseDomain}"), new Cookie(name, val));
            }
            catch { }
        }
    }
}