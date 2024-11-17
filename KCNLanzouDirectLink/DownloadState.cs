namespace KCNLanzouDirectLink
{
    /// <summary>
    /// 操作状态枚举类
    /// </summary>
    public enum DownloadState
    {
        /// <summary>
        /// 操作成功完成。
        /// </summary>
        Success,

        /// <summary>
        /// 未提供有效的分享链接。
        /// </summary>
        UrlNotProvided,

        /// <summary>
        /// 无法获取网页内容。分享链接无效？
        /// </summary>
        HtmlContentNotFound,

        /// <summary>
        /// 无法解析加密信息。分享链接无效或密钥错误？
        /// </summary>
        PostsignNotFound,

        /// <summary>
        /// 无法解析中间链接。
        /// </summary>
        IntermediateUrlNotFound,

        /// <summary>
        /// 无法获取最终的直链地址。
        /// </summary>
        FinalUrlNotFound,

        /// <summary>
        /// 未知错误，操作未成功完成。
        /// </summary>
        Error
    }
}
