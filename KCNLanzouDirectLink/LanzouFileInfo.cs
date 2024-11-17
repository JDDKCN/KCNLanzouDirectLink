namespace KCNLanzouDirectLink
{
    /// <summary>
    /// 文件信息结构类
    /// </summary>
    public class LanzouFileInfo
    {
        /// <summary>
        /// 文件名称
        /// </summary>
        public string? FileName { get; set; }
        /// <summary>
        /// 上传时间
        /// </summary>
        public string? UploadTime { get; set; }
        /// <summary>
        /// 文件大小
        /// </summary>
        public string? Size { get; set; }
        /// <summary>
        /// 上传者
        /// </summary>
        public string? Uploader { get; set; }
        /// <summary>
        /// 运行平台
        /// </summary>
        public string? Platform { get; set; }
        /// <summary>
        /// 文件描述
        /// </summary>
        public string? Description { get; set; }
    }
}
