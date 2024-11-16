namespace KCNLanzouDirectLink
{
    internal class LanzouEncryptedFileInfo
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 文件下载域名
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// 文件拼接URL
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        public string? FileName { get; set; }
    }
}
