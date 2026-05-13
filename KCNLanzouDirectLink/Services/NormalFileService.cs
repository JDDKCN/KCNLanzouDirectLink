using KCNLanzouDirectLink.Core;
using KCNLanzouDirectLink.Models;
using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace KCNLanzouDirectLink.Services
{
    /// <summary>
    /// 普通无密码文件服务
    /// </summary>
    internal class NormalFileService : LanzouHttpClient
    {
        /// <summary>
        /// 获取普通文件直链
        /// </summary>
        public async Task<(DownloadState State, string? Url)> GetDirectLinkAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return (DownloadState.UrlNotProvided, null);

            if (!InitializeDomain(url))
                return (DownloadState.UrlNotProvided, null);

            var htmlContent = await GetDownloadPageContentAsync(url);
            if (htmlContent == null)
                return (DownloadState.HtmlContentNotFound, null);

            var intermediateUrl = await ExtractDownloadLinkAsync(htmlContent);
            if (intermediateUrl == null)
                return (DownloadState.IntermediateUrlNotFound, null);

            var (success, finalUrl) = await GetRedirectUrlAsync(intermediateUrl);
            return success && !string.IsNullOrEmpty(finalUrl)
                ? (DownloadState.Success, finalUrl)
                : (DownloadState.FinalUrlNotFound, null);
        }

        /// <summary>
        /// 获取文件详细信息
        /// </summary>
        public async Task<(DownloadState State, LanzouFileInfo? FileInfo)> GetFileInfoAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return (DownloadState.UrlNotProvided, null);

            if (!InitializeDomain(url))
                return (DownloadState.UrlNotProvided, null);

            try
            {
                var htmlContent = await GetInfoPageContentAsync(url);
                if (string.IsNullOrWhiteSpace(htmlContent))
                    return (DownloadState.HtmlContentNotFound, null);

                var fileInfo = ParseFileInfo(htmlContent);
                return IsValidFileInfo(fileInfo)
                    ? (DownloadState.Success, fileInfo)
                    : (DownloadState.Error, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取文件信息异常: {ex.Message}");
                return (DownloadState.Error, null);
            }
        }

        /// <summary>
        /// 获取下载页面内容
        /// <para>支持iframe和AJAX</para>
        /// </summary>
        private async Task<string?> GetDownloadPageContentAsync(string url)
        {
            var content = await FetchMainPageAsync(url);
            if (content == null)
                return null;

            // 检查是否已包含传统下载变量
            if (HasTraditionalDownloadVariables(content))
                return content;

            // 尝试处理iframe页面
            var iframeContent = await TryProcessIframePageAsync(content, url);
            if (iframeContent != null)
                return iframeContent;

            // 尝试处理传统跳转
            var redirectContent = await TryProcessTraditionalRedirectAsync(content, url);
            if (redirectContent != null)
                return redirectContent;

            return content;
        }

        /// <summary>
        /// 获取信息页面内容
        /// <para>不处理跳转</para>
        /// </summary>
        private async Task<string?> GetInfoPageContentAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            SetCommonHeaders(request);
            return await SendRequestWithAntiCrawlerAsync(request);
        }

        /// <summary>
        /// 获取主页面
        /// </summary>
        private async Task<string?> FetchMainPageAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            SetCommonHeaders(request);
            return await SendRequestWithAntiCrawlerAsync(request);
        }

        /// <summary>
        /// 尝试处理iframe页面
        /// </summary>
        private async Task<string?> TryProcessIframePageAsync(string mainPageContent, string mainPageUrl)
        {
            var iframeMatch = Regex.Match(mainPageContent, @"<iframe[^>]+src=""(/fn\?[^""]+)""");
            if (!iframeMatch.Success || _domainInfo == null)
                return null;

            var iframePath = iframeMatch.Groups[1].Value;
            var iframeUrl = $"{_domainInfo.BaseUrl}{iframePath}";
            var iframeContent = await FetchIframeContentAsync(iframeUrl, mainPageUrl);

            if (iframeContent == null)
                return null;

            var fileId = ExtractFileId(iframeContent);

            if (IsAjaxDynamicPage(iframeContent))
                return BuildAjaxMarker(iframeContent, fileId, iframeUrl);

            if (HasTraditionalDownloadVariables(iframeContent))
                return iframeContent;

            return iframeContent;
        }

        /// <summary>
        /// 获取iframe内容
        /// </summary>
        private async Task<string?> FetchIframeContentAsync(string iframeUrl, string refererUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, iframeUrl);
            SetCommonHeaders(request);

            if (Uri.TryCreate(refererUrl, UriKind.Absolute, out var uri))
                request.Headers.Referrer = uri;

            return await SendRequestWithAntiCrawlerAsync(request);
        }

        /// <summary>
        /// 构建AJAX标记
        /// </summary>
        private string? BuildAjaxMarker(string iframeContent, string? fileId, string iframeUrl)
        {
            var signMatch = Regex.Match(iframeContent, @"var\s+wp_sign\s*=\s*['""]([^'""]+)['""]");
            var ajaxDataMatch = Regex.Match(iframeContent, @"var\s+ajaxdata\s*=\s*['""]([^'""]+)['""]");

            if (signMatch.Success && !string.IsNullOrEmpty(fileId))
            {
                var sign = signMatch.Groups[1].Value;
                var ajaxdata = ajaxDataMatch.Success ? ajaxDataMatch.Groups[1].Value : "rewn";
                return $"AJAX|{sign}|{fileId}|{ajaxdata}|{iframeUrl}";
            }
            return null;
        }

        /// <summary>
        /// 尝试处理传统跳转
        /// </summary>
        private async Task<string?> TryProcessTraditionalRedirectAsync(string content, string currentUrl)
        {
            var match = Regex.Match(content, @"<div class=""mh""><a href=""([^""]+)""");
            if (!match.Success)
                return null;

            var tpPath = match.Groups[1].Value;
            if (string.IsNullOrEmpty(tpPath))
                return null;

            var tpUrl = new Uri(new Uri(currentUrl), tpPath).ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, tpUrl);
            SetCommonHeaders(request);

            return await SendRequestWithAntiCrawlerAsync(request);
        }

        /// <summary>
        /// 提取下载链接
        /// </summary>
        private async Task<string?> ExtractDownloadLinkAsync(string htmlContent)
        {
            // AJAX方式
            if (htmlContent.StartsWith("AJAX|"))
                return await ProcessAjaxDownloadAsync(htmlContent);

            // 传统方式
            return ExtractTraditionalDownloadLink(htmlContent);
        }

        /// <summary>
        /// 处理AJAX下载
        /// </summary>
        private async Task<string?> ProcessAjaxDownloadAsync(string ajaxMarker)
        {
            var parts = ajaxMarker.Split('|');
            if (parts.Length != 5)
                return null;

            var sign = parts[1];
            var fileId = parts[2];
            var ajaxdata = parts[3];
            var iframeUrl = parts[4];

            return await FetchAjaxDownloadUrlAsync(sign, fileId, ajaxdata, iframeUrl);
        }

        /// <summary>
        /// 提取传统下载链接
        /// </summary>
        private string? ExtractTraditionalDownloadLink(string htmlContent)
        {
            var vkjxld = ExtractPattern(htmlContent, @"var\s*vkjxld\s*=\s*['""]([^'""]+)['""]");
            var hyggid = ExtractPattern(htmlContent, @"var\s*hyggid\s*=\s*['""]([^'""]+)['""]");

            if (vkjxld == null || hyggid == null)
                return null;

            return vkjxld + hyggid;
        }

        /// <summary>
        /// 通过AJAX获取下载URL
        /// </summary>
        private async Task<string?> FetchAjaxDownloadUrlAsync(string sign, string fileId, string ajaxdata, string refererUrl)
        {
            if (_domainInfo == null)
                return null;

            var ajaxUrl = $"{_domainInfo.BaseUrl}/ajaxm.php?file={fileId}";
            var postData = BuildAjaxPostData(sign, ajaxdata);

            var request = new HttpRequestMessage(HttpMethod.Post, ajaxUrl);
            SetCommonHeaders(request);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            if (Uri.TryCreate(refererUrl, UriKind.Absolute, out var uri))
                request.Headers.Referrer = uri;

            // 前端风控 Cookie
            var baseUri = new Uri(_domainInfo.BaseUrl);
            _cookieContainer.Add(baseUri, new Cookie("pc_ad1", "1"));
            _cookieContainer.Add(baseUri, new Cookie("codelen", "1"));

            var content = new FormUrlEncodedContent(postData);
            content.Headers.ContentType!.CharSet = string.Empty;
            request.Content = content;

            Debug.WriteLine($"[Ajax Post] URL: {ajaxUrl}");
            Debug.WriteLine($"[Ajax Post] Data: {await request.Content.ReadAsStringAsync()}");

            var responseContent = await SendRequestWithAntiCrawlerAsync(request, postData);

            Debug.WriteLine($"[Ajax Response]: {responseContent}");

            if (responseContent == null || !responseContent.TrimStart().StartsWith("{"))
                return null;

            return ParseAjaxResponse(responseContent);
        }

        /// <summary>
        /// 构建AJAX POST数据
        /// </summary>
        private Dictionary<string, string> BuildAjaxPostData(string sign, string ajaxdata)
        {
            return new Dictionary<string, string>
            {
                ["action"] = "downprocess",
                ["websignkey"] = ajaxdata,
                ["signs"] = ajaxdata,
                ["sign"] = sign,
                ["websign"] = "",
                ["kd"] = "1",
                ["ves"] = "1"
            };
        }

        /// <summary>
        /// 解析AJAX响应
        /// </summary>
        private string? ParseAjaxResponse(string jsonContent)
        {
            try
            {
                var jsonResponse = JsonNode.Parse(jsonContent);
                if (jsonResponse == null)
                    return null;

                int status = 0;
                var ztNode = jsonResponse["zt"];

                if (ztNode != null)
                {
                    if (!ztNode.AsValue().TryGetValue<int>(out status))
                    {
                        if (ztNode.AsValue().TryGetValue<string>(out var strVal))
                        {
                            _ = int.TryParse(strVal, out status);
                        }
                    }
                }

                var domain = jsonResponse["dom"]?.ToString();
                var url = jsonResponse["url"]?.ToString();

                if (status == 1 && !string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(url))
                    return $"{domain}/file/{url}";

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析文件信息
        /// </summary>
        private LanzouFileInfo ParseFileInfo(string htmlContent)
        {
            return new LanzouFileInfo
            {
                Status = 1,
                FileName = ExtractValue(htmlContent, @"<div style=""font-size: 30px;text-align: center;padding: 56px 0px 20px 0px;"">(.*?)</div>"),
                Size = ExtractValue(htmlContent, @"<span class=""p7"">文件大小：</span>(.*?)<br>"),
                UploadTime = ExtractValue(htmlContent, @"<span class=""p7"">上传时间：</span>(.*?)<br>"),
                Uploader = ExtractValue(htmlContent, @"<span class=""p7"">分享用户：</span><font>(.*?)</font><br>"),
                Platform = ExtractValue(htmlContent, @"<span class=""p7"">运行系统：</span>(.*?)<br>"),
                Description = ExtractValue(htmlContent, @"<span class=""p7"">文件描述：</span>(.*?)<br>").Trim()
            };
        }

        /// <summary>
        /// 验证文件信息是否有效
        /// </summary>
        private bool IsValidFileInfo(LanzouFileInfo fileInfo)
        {
            return !string.IsNullOrEmpty(fileInfo.FileName) ||
                   !string.IsNullOrEmpty(fileInfo.Size) ||
                   !string.IsNullOrEmpty(fileInfo.UploadTime) ||
                   !string.IsNullOrEmpty(fileInfo.Uploader) ||
                   !string.IsNullOrEmpty(fileInfo.Platform) ||
                   !string.IsNullOrEmpty(fileInfo.Description);
        }

        /// <summary>
        /// 检查是否包含传统下载变量
        /// </summary>
        private bool HasTraditionalDownloadVariables(string content)
        {
            return content.Contains("vkjxld") && content.Contains("hyggid");
        }

        /// <summary>
        /// 检查是否是AJAX动态页面
        /// </summary>
        private bool IsAjaxDynamicPage(string content)
        {
            return content.Contains("$.ajax") && content.Contains("ajaxm.php");
        }

        /// <summary>
        /// 从主页面提取文件ID
        /// </summary>
        private string? ExtractFileId(string htmlContent)
        {
            // 从JavaScript变量提取
            var jsMatch = Regex.Match(htmlContent, @"var\s+fid\s*=\s*(\d+)");
            if (jsMatch.Success)
                return jsMatch.Groups[1].Value;

            // 从ajaxm.php引用提取，至少6位数字
            var ajaxMatch = Regex.Match(htmlContent, @"/ajaxm\.php\?file=(\d{6,})");
            if (ajaxMatch.Success)
                return ajaxMatch.Groups[1].Value;

            return null;
        }

        /// <summary>
        /// 正则提取单个值
        /// </summary>
        private string? ExtractPattern(string content, string pattern)
        {
            try
            {
                var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 提取HTML值
        /// </summary>
        private string ExtractValue(string htmlContent, string pattern)
        {
            var match = Regex.Match(htmlContent, pattern, RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }
    }
}