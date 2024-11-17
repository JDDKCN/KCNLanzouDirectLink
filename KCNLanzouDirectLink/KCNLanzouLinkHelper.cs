namespace KCNLanzouDirectLink
{
    /// <summary>
    /// 解析蓝奏云直链
    /// </summary>
    public static class KCNLanzouLinkHelper
    {
        /// <summary>
        /// 获取蓝奏云分享链接的直链。
        /// </summary>
        /// <param name="url">蓝奏云分享链接</param>
        /// <returns>返回解析后的直链，若失败则返回 null</returns>
        public static async Task<(DownloadState State, string? Url)> GetDirectLinkAsync(string url)
        {
            return await GetUrlHelper.GetFullUrl(url);
        }

        /// <summary>
        /// 获取加密蓝奏云分享链接的直链。
        /// </summary>
        /// <param name="url">蓝奏云分享链接</param>
        /// <param name="key">加密密钥</param>
        /// <param name="readyNum">重试次数(默认为0，不重试)</param>
        /// <returns>返回解析后的直链，若失败则返回 null</returns>
        public static async Task<(DownloadState State, string? Url)> GetDirectLinkAsync(string url, string key, int readyNum = 0)
        {
            if (readyNum <= 0)
                return await GetUrlHelper.GetFullUrl(url, key);

            (DownloadState state, string? link) result = (DownloadState.Error, null);
            for (int i = 0; i < 10; i++)
            {
                result = await GetUrlHelper.GetFullUrl(url, key);
                if (result.state == DownloadState.Success)
                    break;
            }

            return result;
        }

        /// <summary>
        /// 批量获取蓝奏云分享链接的直链（支持普通链接和加密链接）。
        /// </summary>
        /// <typeparam name="T">输入集合类型。支持 string(普通分享链接url) 或 Tuple&lt;string, string&gt;(加密分享链接url，对应密钥)。</typeparam>
        /// <param name="urls">蓝奏云分享链接集合。</param>
        /// <param name="readyNum">加密链接重试次数(0为不重试)</param>
        /// <returns>返回每个链接的解析结果，包括状态和直链。</returns>
        public static async Task<List<(string Url, DownloadState State, string? DirectLink)>> GetDirectLinksAsync<T>(IEnumerable<T> urls, int readyNum = 10)
        {
            var results = new List<(string Url, DownloadState State, string? DirectLink)>();

            foreach (var item in urls)
            {
                string url;
                string? key = null;

                if (item is string plainUrl)
                {
                    url = plainUrl;
                }
                else if (item is Tuple<string, string> encryptedUrl)
                {
                    url = encryptedUrl.Item1;
                    key = encryptedUrl.Item2;
                }
                else
                {
                    throw new ArgumentException("Unsupported URL type - 不支持的类型。该泛型仅允许实现 string 及 Tuple<string, string>。");
                }

                (var state, var directLink) = string.IsNullOrWhiteSpace(key)
                    ? await GetDirectLinkAsync(url)
                    : await GetDirectLinkAsync(url, key, readyNum);

                results.Add((url, state, directLink));
            }

            return results;
        }

        /// <summary>
        /// 获取蓝奏云分享链接的信息。
        /// </summary>
        /// <param name="url">蓝奏云分享链接</param>
        /// <returns></returns>
        public static async Task<(DownloadState State, LanzouFileInfo? FileInfo)> GetFileInfoAsync(string url)
        {
            return await GetUrlInfoHelper.ParseFileInfoAsync(false, url);
        }

        /// <summary>
        /// 获取蓝奏云加密分享链接的信息。
        /// </summary>
        /// <param name="url">蓝奏云分享链接</param>
        /// <param name="key">加密密钥</param>
        /// <returns></returns>
        public static async Task<(DownloadState State, LanzouFileInfo? FileInfo)> GetFileInfoAsync(string url, string key)
        {
            return await GetUrlInfoHelper.ParseFileInfoAsync(true, url, key);
        }

        /// <summary>
        /// (显式指定)获取蓝奏云分享链接的信息。
        /// </summary>
        /// <param name="isEncryption">是否为加密资源</param>
        /// <param name="url">蓝奏云分享链接</param>
        /// <param name="key">加密密钥(非必须)</param>
        /// <returns></returns>
        public static async Task<(DownloadState State, LanzouFileInfo? FileInfo)> GetFileInfoAsync(bool isEncryption, string url, string key = "")
        {
            return await GetUrlInfoHelper.ParseFileInfoAsync(isEncryption, url, key);
        }

    }
}
