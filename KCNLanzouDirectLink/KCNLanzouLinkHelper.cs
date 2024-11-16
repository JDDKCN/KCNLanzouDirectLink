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
        /// <returns>返回解析后的直链，若失败则返回 null</returns>
        public static async Task<(DownloadState State, string? Url)> GetDirectLinkAsync(string url, string key)
        {
            return await GetUrlHelper.GetFullUrl(url, key);
        }

        /// <summary>
        /// 批量获取蓝奏云分享链接的直链。
        /// </summary>
        /// <param name="urls">蓝奏云分享链接集合</param>
        /// <returns>返回每个链接的解析结果，包括状态和直链。</returns>
        public static async Task<List<(string Url, DownloadState State, string? DirectLink)>> GetDirectLinksAsync(IEnumerable<string> urls)
        {
            var results = new List<(string Url, DownloadState State, string? DirectLink)>();

            foreach (var url in urls)
            {
                var (state, directLink) = await GetDirectLinkAsync(url);
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
