# KCNLanzouDirectLink

[![NuGet](https://img.shields.io/nuget/v/KCNLanzouDirectLink.svg)](https://www.nuget.org/packages/KCNLanzouDirectLink/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/KCNLanzouDirectLink.svg)](https://www.nuget.org/packages/KCNLanzouDirectLink/)

`KCNLanzouDirectLink` 是一个用于解析蓝奏云分享链接并获取直链的 C# 原生实现类库。它提供了简单的 API 用于获取蓝奏云分享链接的直链，无需登录或第三方工具。

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
        string? fullUrl = await GetUrlHelper.GetFullUrl(shareUrl);

        Console.WriteLine($"直链地址: {fullUrl}");
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
        string shareUrl = "https://syxz.lanzoue.com/i4MRg1emxw9c";
        string key = "your_encryption_key";  // 使用你获取到的加密key
        string? fullUrl = await GetUrlHelper.GetFullUrl(shareUrl, key);

        Console.WriteLine($"直链地址: {fullUrl}");
    }
}
```

## 许可协议

该项目使用 [A-GPLv3](https://opensource.org/licenses/AGPL-3.0) 许可协议。

## 贡献

欢迎提出问题、改进建议或直接提交 Pull Request！

## 联系方式

- [前往我的Github](https://github.com/JDDKCN)
- [前往我的B站首页](https://space.bilibili.com/475547854/)
- [前往我的Twitter账号](https://twitter.com/2233KCN03)