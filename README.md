# KCNLanzouDirectLink

[![NuGet](https://img.shields.io/nuget/v/KCNLanzouDirectLink.svg)](https://www.nuget.org/packages/KCNLanzouDirectLink/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/KCNLanzouDirectLink.svg)](https://www.nuget.org/packages/KCNLanzouDirectLink/)

`KCNLanzouDirectLink` 是一个用于解析蓝奏云分享链接并获取直链的 C# 原生实现类库。它提供了简单的 API 用于获取蓝奏云分享链接的直链，无需登录或第三方工具。

## 项目特点

- 支持解析蓝奏云分享直链 & 加密分享直链。
- 支持解析分享 & 加密分享链接的文件信息。
- 不使用curl，原生实现所有功能。
- 提供标准错误处理模式。
- 完整的实现 Demo，快速上手。

## 支持

本项目支持以下 .NET 版本：

- `.NET 6`、`.NET 7`、`.NET 8`、以及更高版本的 .NET。


## 安装

你可以通过 NuGet 包管理器安装 `KCNLanzouDirectLink` 库：

```bash
Install-Package KCNLanzouDirectLink
```

或者通过 .NET CLI：

```bash
dotnet add package KCNLanzouDirectLink
```

## 使用示例

获取普通链接的直链

```csharp
using KCNLanzouDirectLink;

class Program
{
    static async Task Main(string[] args)
    {
        string shareUrl = "https://syxz.lanzoue.com/qwertyuiopas";
        string? link = await KCNLanzouLinkHelper.GetDirectLinkAsync(shareUrl);

        Console.WriteLine($"直链地址: {link}");
    }
}
```

获取加密链接的直链

```csharp
using KCNLanzouDirectLink;

class Program
{
    static async Task Main(string[] args)
    {
        string shareUrl = "https://syxz.lanzoue.com/qwertyuiopas";
        string key = "your_encryption_key";  
        var (state, linkEncryption) = await KCNLanzouLinkHelper.GetFullUrl(shareUrl, key);

        if (state == DownloadState.Success)
        {
            Console.WriteLine($"直链地址: {linkEncryption}");
        }
        else
        {
            Console.WriteLine($"获取直链失败，状态: {state}");
        }
    }
}
```

获取链接的文件信息

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        string shareUrl = "https://syxz.lanzoue.com/qwertyuiopas";
        string key = "your_encryption_key";  

        // true表示强制指定分享链接为加密链接类型。具体信息请查看方法重载。
        var (state, fileInfo) = await KCNLanzouLinkHelper.GetFileInfoAsync(true, shareUrl, key); 

        if (state == DownloadState.Success)
        {
            string message = $"文件信息解析成功：\n" +
                $"File info retrieved successfully:\n" +
                $"文件名称\\File Name: {fileInfo.FileName}\n" +
                $"文件大小\\File Size: {fileInfo.Size}\n" +
                $"上传时间\\Upload Time: {fileInfo.UploadTime}\n" +
                $"上传者\\Uploader: {fileInfo.Uploader}\n" +
                $"运行平台\\Platform: {fileInfo.Platform}\n" +
                $"文件描述\\Description: {fileInfo.Description}";

            Console.WriteLine($"直链地址: {message}");
        }
        else
        {
            Console.WriteLine($"获取文件信息失败，状态: {state}");
        }
    }
}
```

枚举 `DownloadState` 说明

```csharp
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
```

文件信息获取结构类 `LanzouFileInfo` 说明

```csharp
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
```

## Demo

请前往Github存储库查看类库Demo：[https://github.com/JDDKCN/KCNLanzouDirectLink](https://github.com/JDDKCN/KCNLanzouDirectLink)

## 许可协议

该项目使用 [A-GPLv3](https://opensource.org/licenses/AGPL-3.0) 许可协议。

## 贡献

欢迎提出问题、改进建议或直接提交 Pull Request！

## 联系方式

- [前往我的Github](https://github.com/JDDKCN)
- [前往我的B站首页](https://space.bilibili.com/475547854/)
- [前往我的Twitter账号](https://twitter.com/2233KCN03)