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
        public static async Task<string?> GetDirectLinkAsync(string url)
        {
            return await GetUrlHelper.GetFullUrl(url);
        }

        /// <summary>
        /// 获取加密蓝奏云分享链接的直链。
        /// </summary>
        /// <param name="url">蓝奏云分享链接</param>
        /// <param name="key">加密密钥</param>
        /// <returns>返回解析后的直链，若失败则返回 null</returns>
        public static async Task<string?> GetDirectLinkAsync(string url, string key)
        {
            return await GetUrlHelper.GetFullUrl(url, key);
        }
    }
}
