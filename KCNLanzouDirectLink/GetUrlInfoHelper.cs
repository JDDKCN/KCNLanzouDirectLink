using System.Text.RegularExpressions;

namespace KCNLanzouDirectLink
{
    internal class GetUrlInfoHelper
    {
        /// <summary>
        /// 解析蓝奏云文件详细信息
        /// </summary>
        public static async Task<(DownloadState State, LanzouFileInfo? FileInfo)> ParseFileInfoAsync(bool isEncryption, string url, string key = "")
        {
            if (string.IsNullOrEmpty(url))
                return (DownloadState.UrlNotProvided, null);

            string? htmlContent;
            string? fileNameToEncryption = string.Empty;

            try
            {
                htmlContent = await GetUrlHelper.GetHtmlContentToInfoAsync(url);

                if (string.IsNullOrWhiteSpace(htmlContent))
                    return (DownloadState.HtmlContentNotFound, null);

                if (isEncryption)
                {
                    var postsign = GetUrlHelper.ExtractPatternValue(await GetUrlHelper.GetHtmlContentAsync(url), @"var\s*vidksek\s*=\s*['""]([^'""]+)['""]");
                    if (postsign != null)
                    {
                        LanzouEncryptedFileInfo? flieConfig = await GetUrlHelper.GetHtmlToPassContentDataAsync(postsign, key);
                        fileNameToEncryption = flieConfig.FileName;
                    }
                }
            }
            catch
            {
                return (DownloadState.Error, null);
            }

            try
            {
                LanzouFileInfo fileInfo;

                if (!isEncryption)
                {
                    fileInfo = new LanzouFileInfo
                    {
                        FileName = ExtractValue(htmlContent, @"<div style=""font-size: 30px;text-align: center;padding: 56px 0px 20px 0px;"">(.*?)</div>"),
                        Size = ExtractValue(htmlContent, @"<span class=""p7"">文件大小：</span>(.*?)<br>"),
                        UploadTime = ExtractValue(htmlContent, @"<span class=""p7"">上传时间：</span>(.*?)<br>"),
                        Uploader = ExtractValue(htmlContent, @"<span class=""p7"">分享用户：</span><font>(.*?)</font><br>"),
                        Platform = ExtractValue(htmlContent, @"<span class=""p7"">运行系统：</span>(.*?)<br>"),
                        Description = ExtractValue(htmlContent, @"<span class=""p7"">文件描述：</span>(.*?)<br>").Trim()
                    };
                }
                else
                {
                    fileInfo = new LanzouFileInfo
                    {
                        FileName = fileNameToEncryption ?? "解析失败",
                        Size = ExtractValue(htmlContent, @"<div class=""n_filesize"">大小：(.*?)</div>"),
                        UploadTime = string.Empty,
                        Uploader = ExtractValue(htmlContent, @"<div class=""passwddiv-user"">获取<span>(.*?)</span>的文件</div>"),
                        Platform = string.Empty,
                        Description = string.Empty,
                    };

                    var fileInfos = ExtractAllValues(htmlContent, @"<span\s+class=""n_file_infos"">(.*?)</span>");

                    if (fileInfos.Count >= 2)
                    {
                        fileInfo.UploadTime = fileInfos[0];
                        fileInfo.Platform = fileInfos[1];
                    }
                }

                if (string.IsNullOrEmpty(fileInfo.FileName) && string.IsNullOrEmpty(fileInfo.Size) &&
                    string.IsNullOrEmpty(fileInfo.UploadTime) && string.IsNullOrEmpty(fileInfo.Uploader) &&
                    string.IsNullOrEmpty(fileInfo.Platform) && string.IsNullOrEmpty(fileInfo.Description))
                    return (DownloadState.Error, null);

                return (DownloadState.Success, fileInfo);
            }
            catch
            {
                return (DownloadState.Error, null);
            }
        }

        /// <summary>
        /// 提取目标值
        /// </summary>
        /// <param name="htmlContent">html</param>
        /// <param name="pattern">正则模式</param>
        /// <returns></returns>
        private static string ExtractValue(string htmlContent, string pattern)
        {
            var match = Regex.Match(htmlContent, pattern, RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        /// <summary>
        /// 提取所有目标值
        /// </summary>
        /// <param name="htmlContent">html</param>
        /// <param name="pattern">正则模式</param>
        /// <returns></returns>
        private static List<string> ExtractAllValues(string htmlContent, string pattern)
        {
            var matches = Regex.Matches(htmlContent, pattern, RegexOptions.Singleline);
            var results = new List<string>();

            foreach (Match match in matches.Cast<Match>())
            {
                results.Add(match.Groups[1].Value.Trim());
            }

            return results;
        }
    }
}
