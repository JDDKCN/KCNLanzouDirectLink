using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace KCNLanzouDirectLink.Core;

/// <summary>
/// 反爬虫处理器
/// </summary>
internal class AntiCrawlerHandler
{
    /// <summary>
    /// 正则超时时间
    /// </summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// posList数组提取正则
    /// </summary>
    /// <remarks>
    /// 特征：= [0x??, 0x??, 0x??, ...]
    /// 匹配赋值号后，紧跟中括号，且内部包含至少5个以0x开头的数字
    /// </remarks>
    private static readonly Regex GenericPosListRegex = new(
            @"=\s*\[((?:\s*0x[0-9a-fA-F]+\s*,?){5,})\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            RegexTimeout
        );

    /// <summary>
    /// 全局字符串提取正则
    /// </summary>
    /// <remarks>
    /// 提取所有单引号或双引号包裹的字符串
    /// </remarks>

    private static readonly Regex GlobalStringLiteralRegex = new(
        @"['""]([a-zA-Z0-9+/=]{30,})['""]",
        RegexOptions.Compiled,
        RegexTimeout
    );

    /// <summary>
    /// 十六进制数值提取正则
    /// </summary>
    /// <remarks>
    /// 匹配格式: 0x1f, 0xa, 0x23
    /// 用于从posList数组字符串中提取所有十六进制数值
    /// </remarks>
    private static readonly Regex HexNumberRegex = new(
        @"0x[0-9a-fA-F]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        RegexTimeout
    );

    /// <summary>
    /// arg1提取正则
    /// </summary>
    private static readonly Regex Arg1Regex = new(
        @"=\s*['""]([0-9a-fA-F]{40})['""]",
        RegexOptions.Compiled,
        RegexTimeout
    );

    /// <summary>
    /// 检测响应内容是否为反爬虫页面
    /// </summary>
    /// <param name="content">HTTP响应内容</param>
    /// <returns>如果是反爬虫页面返回true，否则返回false</returns>
    /// <remarks>
    /// 检测条件:
    /// 1. 内容以HTML标签开头（忽略空格）
    /// 2. 包含特征字符串 "var arg1="
    /// </remarks>
    public bool IsAntiCrawlerResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return content.TrimStart().StartsWith("<", StringComparison.OrdinalIgnoreCase)
               && content.Contains("var arg1=", StringComparison.Ordinal);
    }

    /// <summary>
    /// 处理反爬虫，返回Cookie字符串
    /// </summary>
    /// <param name="htmlContent">反爬虫页面的HTML内容</param>
    /// <returns>
    /// 成功时返回Cookie字符串（格式: "acw_sc__v2=xxxxxx"）
    /// 失败时返回null
    /// </returns>
    public string? HandleAntiCrawler(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            Debug.WriteLine("HTML内容为空");
            return null;
        }

        try
        {
            // 1. 提取arg1
            var arg1 = ExtractArg1(htmlContent);
            if (arg1 == null)
            {
                Debug.WriteLine("无法提取arg1参数");
                return null;
            }

            Debug.WriteLine($"提取到arg1: {arg1}");

            // 2. 提取posList数组
            var posList = ExtractPosList(htmlContent);
            if (posList == null || posList.Length == 0)
            {
                Debug.WriteLine("无法提取posList数组");
                return null;
            }

            Debug.WriteLine($"提取到posList: [{string.Join(", ", posList.Select(p => $"0x{p:x}"))}]");

            // 3. 提取mask值
            var mask = ExtractMask(htmlContent);
            if (string.IsNullOrEmpty(mask))
            {
                Debug.WriteLine("无法提取mask掩码");
                return null;
            }

            Debug.WriteLine($"提取到mask: {mask} (长度: {mask.Length})");

            // 4. 计算Cookie值
            var cookieValue = CalculateAcwScV2Cookie(arg1, posList, mask);
            if (string.IsNullOrEmpty(cookieValue))
            {
                Debug.WriteLine("计算Cookie失败");
                return null;
            }

            var result = $"acw_sc__v2={cookieValue}";
            Debug.WriteLine($"生成Cookie: {result}");

            return result;
        }
        catch (RegexMatchTimeoutException ex)
        {
            Debug.WriteLine($"正则表达式超时: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"处理反爬虫页面异常: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从HTML内容中提取arg1参数
    /// </summary>
    /// <param name="htmlContent">HTML内容</param>
    /// <returns>成功返回40位十六进制字符串，失败返回null</returns>
    /// <remarks>
    /// arg1是加密算法的输入参数，通常为40位SHA-1哈希值的十六进制表示
    /// </remarks>
    private string? ExtractArg1(string htmlContent)
    {
        try
        {
            var matches = Arg1Regex.Matches(htmlContent);

            foreach (Match match in matches)
            {
                var val = match.Groups[1].Value;

                // TODO: 数组校验
                return val;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从HTML内容中提取posList数组
    /// </summary>
    /// <param name="htmlContent">HTML内容</param>
    /// <returns>成功返回整数数组，失败返回null</returns>
    /// <remarks>
    /// posList数组定义了字符位置的重排映射关系，用于第一步加密变换
    /// 数组中的值表示原始字符串中字符的索引位置（从1开始）
    /// </remarks>
    private int[]? ExtractPosList(string htmlContent)
    {
        try
        {
            var matches = GenericPosListRegex.Matches(htmlContent);

            foreach (Match match in matches)
            {
                var content = match.Groups[1].Value;

                var hexMatches = HexNumberRegex.Matches(content);
                if (hexMatches.Count < 10) continue;

                var list = new int[hexMatches.Count];
                for (int i = 0; i < hexMatches.Count; i++)
                {
                    list[i] = Convert.ToInt32(hexMatches[i].Value, 16);
                }

                // TODO: 数组校验
                return list;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从HTML内容中提取mask掩码值
    /// </summary>
    /// <param name="htmlContent">HTML内容</param>
    /// <returns>成功返回掩码字符串，失败返回null</returns>
    /// <remarks>
    /// mask掩码用于XOR加密的第二步，通常存储在Base64编码的数组中。
    /// </remarks>
    private string? ExtractMask(string htmlContent)
    {
        try
        {
            var matches = GlobalStringLiteralRegex.Matches(htmlContent);
            if (matches.Count == 0) return null;

            string? bestCandidate = null;
            int maxLength = 0;

            // 阿里云盾自有 base64 字典表
            const string AliyunDict = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+/=";

            foreach (Match match in matches)
            {
                var val = match.Groups[1].Value;

                if (val == AliyunDict) continue;

                if (val.Length > maxLength)
                {
                    maxLength = val.Length;
                    bestCandidate = val;
                }
            }

            if (bestCandidate == null) return null;

            Debug.WriteLine($"盲提取到加密Mask: {bestCandidate}");

            // 需要将阿里云盾 Base64 转换为标准 Base64
            // 阿里云盾字典小写在前，大写在后。标准大写在前，小写在后。需要翻转。
            var standardBase64Builder = new StringBuilder(bestCandidate.Length);
            foreach (char c in bestCandidate)
            {
                if (c >= 'a' && c <= 'z')
                    standardBase64Builder.Append((char)(c - 'a' + 'A')); // 小写转大写
                else if (c >= 'A' && c <= 'Z')
                    standardBase64Builder.Append((char)(c - 'A' + 'a')); // 大写转小写
                else
                    standardBase64Builder.Append(c); // 数字和符号不变
            }

            var standardBase64 = standardBase64Builder.ToString();
            Debug.WriteLine($"转换为标准Mask: {standardBase64}");

            // 修正 Padding ，补全 '='
            int mod4 = standardBase64.Length % 4;
            if (mod4 > 0)
            {
                standardBase64 += new string('=', 4 - mod4);
            }

            // 标准 Base64 解码
            var maskBytes = Convert.FromBase64String(standardBase64);
            var result = Encoding.UTF8.GetString(maskBytes);

            Debug.WriteLine($"最终解码Mask: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Mask提取/解码异常: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 计算acw_sc__v2 Cookie值
    /// </summary>
    /// <param name="arg1">输入字符串（40位十六进制）</param>
    /// <param name="posList">位置重排数组</param>
    /// <param name="mask">XOR加密掩码</param>
    /// <returns>Cookie值（十六进制字符串）</returns>
    /// <remarks>
    /// 加密算法分为两步:
    /// 1. 位置重排: 根据posList数组重新排列arg1的字符顺序
    /// 2. XOR加密: 将重排后的字符串与mask进行异或运算
    /// </remarks>
    private string CalculateAcwScV2Cookie(string arg1, int[] posList, string mask)
    {
        try
        {
            // 位置重排
            var arg2 = PerformPositionRearrangement(arg1, posList);
            if (string.IsNullOrEmpty(arg2))
            {
                Debug.WriteLine("位置重排失败");
                return string.Empty;
            }

            Debug.WriteLine($"位置重排结果: {arg2}");

            // XOR加密
            var arg3 = PerformXorEncryption(arg2, mask);

            Debug.WriteLine($"XOR加密结果: {arg3}");

            return arg3;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"计算Cookie异常: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 执行位置重排操作
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="posList">位置映射数组</param>
    /// <returns>重排后的字符串</returns>
    private string PerformPositionRearrangement(string input, int[] posList)
    {
        try
        {
            var outputList = new char[posList.Length];

            // (O(n))实现
            for (int targetIndex = 0; targetIndex < posList.Length; targetIndex++)
            {
                int sourceIndex = posList[targetIndex] - 1;
                if (sourceIndex >= 0 && sourceIndex < input.Length)
                {
                    outputList[targetIndex] = input[sourceIndex];
                }
            }

            return new string(outputList);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"位置重排异常: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// 执行XOR加密操作
    /// </summary>
    /// <param name="input">输入字符串（十六进制）</param>
    /// <param name="mask">掩码字符串（十六进制）</param>
    /// <returns>加密后的字符串（十六进制）</returns>
    private string PerformXorEncryption(string input, string mask)
    {
        try
        {
            var result = new StringBuilder();

            // 每次处理2字符/1字节
            for (int i = 0; i < input.Length && i < mask.Length; i += 2)
            {
                // 1. 提取2位十六进制字符
                var inputHex = input.Substring(i, 2);
                var maskHex = mask.Substring(i, 2);

                // 2. 转换为整数
                var inputByte = Convert.ToInt32(inputHex, 16);
                var maskByte = Convert.ToInt32(maskHex, 16);

                // 3. XOR运算
                var xorResult = inputByte ^ maskByte;

                // 4. 转换回十六进制，确保2位
                var xorHex = xorResult.ToString("x2");

                result.Append(xorHex);
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"XOR加密异常: {ex.Message}");
            return string.Empty;
        }
    }
}