namespace KCNLanzouDirectLink.Models;

/// <summary>
/// 蓝奏云域名信息
/// </summary>
internal class LanzouDomainInfo
{
    /// <summary>
    /// 完整域名
    /// </summary>
    public string FullDomain { get; set; } = string.Empty;

    /// <summary>
    /// 一级主域名
    /// </summary>
    public string BaseDomain { get; set; } = string.Empty;

    /// <summary>
    /// 二级子域名
    /// </summary>
    public string? Subdomain { get; set; }

    /// <summary>
    /// Http(s)协议
    /// </summary>
    public string Protocol { get; set; } = "https";

    /// <summary>
    /// 基础主URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 原始URL
    /// </summary>
    public string OriginalUrl { get; set; } = string.Empty;
}