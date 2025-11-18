using System.Diagnostics;
using System.Text.RegularExpressions;

namespace KCNLanzouDirectLink.Core;

/// <summary>
/// 反爬虫处理器
/// </summary>
internal class AntiCrawlerHandler
{
    /// <summary>
    /// 检测是否是反爬虫响应
    /// </summary>
    public bool IsAntiCrawlerResponse(string content)
    {
        return content.TrimStart().StartsWith("<") && content.Contains("var arg1=");
    }

    /// <summary>
    /// 处理反爬虫，返回Cookie字符串
    /// </summary>
    public Task<string?> HandleAntiCrawlerAsync(string htmlContent)
    {
        // 提取arg1
        var arg1Match = Regex.Match(htmlContent, @"var\s+arg1\s*=\s*['""]([A-F0-9]+)['""]");
        if (!arg1Match.Success)
        {
            Debug.WriteLine("无法提取arg1");
            return Task.FromResult<string?>(null);
        }

        var arg1 = arg1Match.Groups[1].Value;
        Debug.WriteLine($"提取到arg1: {arg1}");

        // 计算Cookie值
        var cookieValue = CalculateAcwScV2Cookie(arg1);
        if (string.IsNullOrEmpty(cookieValue))
        {
            Debug.WriteLine("计算Cookie失败");
            return Task.FromResult<string?>(null);
        }

        Debug.WriteLine($"计算得到Cookie: acw_sc__v2={cookieValue}");
        return Task.FromResult<string?>($"acw_sc__v2={cookieValue}");
    }

    /// <summary>
    /// 计算acw_sc__v2 Cookie值
    /// </summary>
    private string? CalculateAcwScV2Cookie(string arg1)
    {
        try
        {
            // 位置重排列表
            int[] posList = new int[] {
                0xf, 0x23, 0x1d, 0x18, 0x21, 0x10, 0x1, 0x26, 0xa, 0x9,
                0x13, 0x1f, 0x28, 0x1b, 0x16, 0x17, 0x19, 0xd, 0x6, 0xb,
                0x27, 0x12, 0x14, 0x8, 0xe, 0x15, 0x20, 0x1a, 0x2, 0x1e,
                0x7, 0x4, 0x11, 0x5, 0x3, 0x1c, 0x22, 0x25, 0xc, 0x24
            };

            // XOR掩码
            string maskBase64 = "MzAwMDE3NjAwMDg1NjAwNjA2MTUwMTUzMzAwMzY5MDAyNzgwMDM3NQ==";
            string mask = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(maskBase64)); // 解Base64

            // 位置重排
            char[] outPutList = new char[posList.Length];
            for (int i = 0; i < arg1.Length && i < posList.Length; i++)
            {
                char thisChar = arg1[i];
                for (int j = 0; j < posList.Length; j++)
                {
                    if (posList[j] == i + 1)
                    {
                        outPutList[j] = thisChar;
                        break;
                    }
                }
            }

            string arg2 = new string(outPutList);

            // XOR运算
            string arg3 = "";
            for (int i = 0; i < arg2.Length && i < mask.Length; i += 2)
            {
                int strChar = Convert.ToInt32(arg2.Substring(i, 2), 16);
                int maskChar = Convert.ToInt32(mask.Substring(i, 2), 16);
                int xorResult = strChar ^ maskChar;
                string xorChar = xorResult.ToString("x2");

                // 确保两位十六进制
                if (xorChar.Length == 1)
                    xorChar = "0" + xorChar;

                arg3 += xorChar;
            }

            return arg3;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"计算Cookie异常: {ex.Message}");
            return null;
        }
    }
}