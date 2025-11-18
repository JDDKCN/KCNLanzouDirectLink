using KCNLanzouDirectLink.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;

namespace KCNLanzouDirectLink.Core;

/// <summary>
/// 蓝奏云HTTP客户端基类 - 处理反爬虫机制
/// </summary>
internal class LanzouHttpClient
{
    protected readonly HttpClient _client;
    protected readonly HttpClient _clientNoRedirect;
    protected readonly AntiCrawlerHandler _antiCrawlerHandler;
    protected LanzouDomainInfo? _domainInfo;

    public LanzouHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = false
        };

        _client = new HttpClient(handler);

        var handlerNoRedirect = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            AllowAutoRedirect = false
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
            Debug.WriteLine($"无效的蓝奏云URL: {url}");
            return false;
        }

        Debug.WriteLine($"解析域名:");
        Debug.WriteLine($"  完整域名: {_domainInfo.FullDomain}");
        Debug.WriteLine($"  基础域名: {_domainInfo.BaseDomain}");
        Debug.WriteLine($"  子域名: {_domainInfo.Subdomain ?? "(无)"}");
        Debug.WriteLine($"  基础URL: {_domainInfo.BaseUrl}");

        return true;
    }

    /// <summary>
    /// 发送请求（自动处理反爬虫和垃圾广告）
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
                Debug.WriteLine("检测到反爬虫机制，开始处理...");

                var cookie = await _antiCrawlerHandler.HandleAntiCrawlerAsync(content);
                if (string.IsNullOrEmpty(cookie))
                {
                    Debug.WriteLine("反爬虫处理失败");
                    return null;
                }

                // 等待后重试
                await Task.Delay(1500);

                // 重新构建请求
                var retryRequest = CloneRequest(request, postData);
                retryRequest.Headers.Add("Cookie", cookie);

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

                await Task.Delay(800);

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
        // 多字符串特征识别
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

        // 复制Headers
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // 重新设置Content
        if (postData != null && original.Method == HttpMethod.Post)
        {
            clone.Content = new FormUrlEncodedContent(postData);
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
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            SetCommonHeaders(request);
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "none");
            request.Headers.Add("Sec-Fetch-User", "?1");

            var response = await _clientNoRedirect.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.Found &&
                response.StatusCode != HttpStatusCode.MovedPermanently)
            {
                return (false, null);
            }

            var location = response.Headers.Location;
            if (location == null)
            {
                return (false, null);
            }

            if (!location.IsAbsoluteUri)
            {
                var baseUri = new Uri(url);
                var newUri = new Uri(baseUri, location);
                return (true, newUri.ToString());
            }

            return (true, location.ToString());
        }
        catch
        {
            return (false, null);
        }
    }
}