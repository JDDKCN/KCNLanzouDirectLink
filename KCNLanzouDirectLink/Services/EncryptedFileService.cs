using KCNLanzouDirectLink.Core;
using KCNLanzouDirectLink.Models;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace KCNLanzouDirectLink.Services
{
    /// <summary>
    /// 加密文件服务
    /// </summary>
    internal class EncryptedFileService : LanzouHttpClient
    {
        /// <summary>
        /// 获取加密文件直链
        /// </summary>
        public async Task<(DownloadState State, string? Url)> GetDirectLinkAsync(string url, string password)
        {
            if (string.IsNullOrEmpty(url))
            {
                return (DownloadState.UrlNotProvided, null);
            }

            if (string.IsNullOrEmpty(password))
            {
                return (DownloadState.PostsignNotFound, null);
            }

            // 初始化域名
            if (!InitializeDomain(url))
            {
                return (DownloadState.UrlNotProvided, null);
            }

            // 获取密码页面HTML
            var htmlContent = await GetPasswordPageAsync(url);
            if (htmlContent == null)
            {
                return (DownloadState.HtmlContentNotFound, null);
            }

            // 提取sign和fileId
            var sign = ExtractSign(htmlContent);
            if (sign == null)
            {
                return (DownloadState.PostsignNotFound, null);
            }

            var fileId = ExtractFileId(htmlContent);

            // POST请求获取文件信息
            var fileInfo = await PostForFileInfoAsync(sign, password, fileId);
            if (fileInfo == null || !fileInfo.IsSuccess)
            {
                return (DownloadState.IntermediateUrlNotFound, null);
            }

            // 构建中间链接
            var intermediateUrl = BuildDownloadUrl(fileInfo.Domain!, fileInfo.Url!);
            Debug.WriteLine($"中间链接: {intermediateUrl}");

            // 获取最终直链
            var (success, finalUrl) = await GetRedirectUrlAsync(intermediateUrl);

            if (success && !string.IsNullOrEmpty(finalUrl))
            {
                Debug.WriteLine($"最终直链: {finalUrl}");
                return (DownloadState.Success, finalUrl);
            }

            return (DownloadState.FinalUrlNotFound, null);
        }

        /// <summary>
        /// 获取加密文件详细信息
        /// </summary>
        public async Task<(DownloadState State, LanzouFileInfo? FileInfo)> GetFileInfoAsync(string url, string password)
        {
            if (string.IsNullOrEmpty(url))
            {
                return (DownloadState.UrlNotProvided, null);
            }

            if (string.IsNullOrEmpty(password))
            {
                return (DownloadState.PostsignNotFound, null);
            }

            // 初始化域名
            if (!InitializeDomain(url))
            {
                return (DownloadState.UrlNotProvided, null);
            }

            try
            {
                // 获取HTML内容（用于基本信息）
                var htmlContent = await GetPasswordPageForInfoAsync(url);
                if (string.IsNullOrWhiteSpace(htmlContent))
                {
                    return (DownloadState.HtmlContentNotFound, null);
                }

                // 提取sign获取文件名
                var passwordHtml = await GetPasswordPageAsync(url);
                if (passwordHtml == null)
                {
                    return (DownloadState.HtmlContentNotFound, null);
                }

                var sign = ExtractSign(passwordHtml);
                var fileId = ExtractFileId(passwordHtml);

                string? fileName = null;
                if (sign != null)
                {
                    var encryptedInfo = await PostForFileInfoAsync(sign, password, fileId);
                    fileName = encryptedInfo?.FileName;
                }

                // 解析基本信息
                var fileInfo = new LanzouFileInfo
                {
                    Status = 1,
                    FileName = fileName ?? "解析失败",
                    Size = ExtractValue(htmlContent, @"<div class=""n_filesize"">大小：(.*?)</div>"),
                    Uploader = ExtractValue(htmlContent, @"<div class=""passwddiv-user"">获取<span>(.*?)</span>的文件</div>"),
                };

                // 提取时间和平台信息
                var fileInfos = ExtractAllValues(htmlContent, @"<span\s+class=""n_file_infos"">(.*?)</span>");
                if (fileInfos.Count >= 2)
                {
                    fileInfo.UploadTime = fileInfos[0];
                    fileInfo.Platform = fileInfos[1];
                }

                // 验证是否成功提取信息
                if (string.IsNullOrEmpty(fileInfo.FileName) && string.IsNullOrEmpty(fileInfo.Size) &&
                    string.IsNullOrEmpty(fileInfo.UploadTime) && string.IsNullOrEmpty(fileInfo.Uploader))
                {
                    return (DownloadState.Error, null);
                }

                return (DownloadState.Success, fileInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取文件信息异常: {ex.Message}");
                return (DownloadState.Error, null);
            }
        }

        /// <summary>
        /// 获取密码页面
        /// </summary>
        private async Task<string?> GetPasswordPageAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            SetCommonHeaders(request);

            var content = await SendRequestWithAntiCrawlerAsync(request);
            if (content == null)
            {
                return null;
            }

            // 检查是否有跳转页面
            var match = Regex.Match(content, @"<div class=""mh""><a href=""([^""]+)""");
            if (match.Success)
            {
                string tpPath = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(tpPath))
                {
                    var baseUri = new Uri(url);
                    var tpUrl = new Uri(baseUri, tpPath).ToString();

                    var tpRequest = new HttpRequestMessage(HttpMethod.Get, tpUrl);
                    SetCommonHeaders(tpRequest);
                    return await SendRequestWithAntiCrawlerAsync(tpRequest);
                }
            }

            return content;
        }

        /// <summary>
        /// 获取密码页面（用于信息提取，不处理跳转）
        /// </summary>
        private async Task<string?> GetPasswordPageForInfoAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            SetCommonHeaders(request);
            return await SendRequestWithAntiCrawlerAsync(request);
        }

        /// <summary>
        /// POST请求获取文件信息
        /// </summary>
        private async Task<LanzouFileInfo?> PostForFileInfoAsync(string sign, string password, string fileId)
        {
            if (_domainInfo == null)
            {
                Debug.WriteLine("域名信息未初始化");
                return null;
            }

            var ajaxUrl = LanzouDomainParser.GetAjaxUrl(_domainInfo, fileId);
            Debug.WriteLine($"AJAX URL: {ajaxUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, ajaxUrl);
            SetCommonHeaders(request);
            request.Headers.Referrer = new Uri(_domainInfo.BaseUrl);

            var postData = new Dictionary<string, string>
            {
                ["action"] = "downprocess",
                ["sign"] = sign,
                ["kd"] = "1",
                ["p"] = password
            };
            request.Content = new FormUrlEncodedContent(postData);

            var content = await SendRequestWithAntiCrawlerAsync(request, postData);
            if (content == null)
            {
                return null;
            }

            // 解析JSON响应
            if (!content.TrimStart().StartsWith("{"))
            {
                Debug.WriteLine("响应不是JSON格式");
                return null;
            }

            try
            {
                var jsonResponse = JObject.Parse(content);

                var fileInfo = new LanzouFileInfo
                {
                    Status = jsonResponse["zt"]?.ToObject<int>() ?? 0,
                    Domain = jsonResponse["dom"]?.ToString(),
                    Url = jsonResponse["url"]?.ToString(),
                    FileName = jsonResponse["inf"]?.ToString()
                };

                Debug.WriteLine($"JSON解析成功: zt={fileInfo.Status}, filename={fileInfo.FileName}");
                return fileInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"JSON解析失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 提取sign（取第二个出现的sign）
        /// </summary>
        private string? ExtractSign(string htmlContent)
        {
            try
            {
                var pattern = @"'sign'\s*:\s*'([^']+)'";
                var matches = Regex.Matches(htmlContent, pattern);

                // 第一个通常在注释中，返回第二个（实际生效的）
                if (matches.Count >= 2)
                {
                    return matches[1].Groups[1].Value;
                }
                else if (matches.Count == 1)
                {
                    return matches[0].Groups[1].Value;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 提取文件ID
        /// </summary>
        private string ExtractFileId(string htmlContent)
        {
            try
            {
                var match = Regex.Match(htmlContent, @"/ajaxm\.php\?file=(\w+)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 构建下载URL
        /// </summary>
        private string BuildDownloadUrl(string domain, string url)
        {
            if (url.StartsWith("?"))
            {
                return $"{domain}/file/{url}";
            }

            if (url.Length > 50 && !url.Contains("/") && !url.StartsWith("?"))
            {
                return $"{domain}/file/?{url}";
            }

            return $"{domain}/file/{url}";
        }

        /// <summary>
        /// 提取单个值
        /// </summary>
        private string ExtractValue(string htmlContent, string pattern)
        {
            var match = Regex.Match(htmlContent, pattern, RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        /// <summary>
        /// 提取所有值
        /// </summary>
        private List<string> ExtractAllValues(string htmlContent, string pattern)
        {
            var matches = Regex.Matches(htmlContent, pattern, RegexOptions.Singleline);
            var results = new List<string>();

            foreach (Match match in matches)
            {
                results.Add(match.Groups[1].Value.Trim());
            }

            return results;
        }
    }
}