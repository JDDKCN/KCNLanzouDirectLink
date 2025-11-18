using KCNLanzouDirectLink.Models;
using System.Text.RegularExpressions;

namespace KCNLanzouDirectLink.Core;

/// <summary>
/// 蓝奏云域名解析器
/// </summary>
internal static class LanzouDomainParser
{
    /// <summary>
    /// 蓝奏云域名正则
    /// </summary>
    private static readonly Regex _domainRegex = new Regex(
        @"https?://(?:www\.)?([^/]+\.)?lanzo[a-z]+\.com",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    /// <summary>
    /// 从URL提取域名信息
    /// </summary>
    public static LanzouDomainInfo? ParseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var match = _domainRegex.Match(url);
        if (!match.Success)
            return null;

        try
        {
            var uri = new Uri(url);

            return new LanzouDomainInfo
            {
                FullDomain = uri.Host,                    // 例: syxz.lanzouw.com
                BaseDomain = GetBaseDomain(uri.Host),     // 例: lanzouw.com
                Subdomain = GetSubdomain(uri.Host),       // 例: syxz
                Protocol = uri.Scheme,                    // 例: https
                BaseUrl = $"{uri.Scheme}://{uri.Host}",   // 例: https://syxz.lanzouw.com
                OriginalUrl = url
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取一级主域名
    /// </summary>
    private static string GetBaseDomain(string host)
    {
        var parts = host.Split('.');
        if (parts.Length >= 2)
        {
            return string.Join(".", parts.Skip(parts.Length - 2));
        }
        return host;
    }

    /// <summary>
    /// 获取二级子域名
    /// </summary>
    private static string? GetSubdomain(string host)
    {
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            return parts[0];
        }
        return null;
    }

    /// <summary>
    /// 验证是否是蓝奏云URL
    /// </summary>
    public static bool IsLanzouUrl(string url)
    {
        return _domainRegex.IsMatch(url);
    }

    /// <summary>
    /// 获取AJAX URL（自动使用正确的域名）
    /// </summary>
    public static string GetAjaxUrl(LanzouDomainInfo domainInfo, string fileId)
    {
        if (!string.IsNullOrEmpty(fileId))
        {
            return $"{domainInfo.BaseUrl}/ajaxm.php?file={fileId}";
        }
        return $"{domainInfo.BaseUrl}/ajaxm.php";
    }
}