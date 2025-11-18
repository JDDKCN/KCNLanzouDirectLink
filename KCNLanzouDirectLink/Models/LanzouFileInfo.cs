namespace KCNLanzouDirectLink.Models;

/// <summary>
/// 蓝奏云文件信息
/// </summary>
public class LanzouFileInfo
{
    // 状态信息

    /// <summary>
    /// 状态码 (1=成功, 0=失败)
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 域名
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// URL路径
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess => Status == 1 && !string.IsNullOrEmpty(Domain) && !string.IsNullOrEmpty(Url);

    // 文件信息

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