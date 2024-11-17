namespace KCNLanzouDirectLink.Demo
{
    public partial class FormMain : Sunny.UI.UIForm
    {
        private static List<Tuple<string, string?>>? linkList;

        public FormMain()
        {
            InitializeComponent();
            linkList = [];
        }

        private async void uiButton1_Click(object sender, EventArgs e)
        {
            if (linkList.Count > 0)
            {
                var results = await KCNLanzouLinkHelper.GetDirectLinksAsync(linkList, 10);
                string text = string.Empty;

                foreach (var (url, state, link) in results)
                {
                    if (state == DownloadState.Success)
                    {
                        text += $"{url} 解析直链地址: {link}\n";
                    }
                    else
                    {
                        text += $"{url} 获取直链失败，状态: {state}\n";
                    }
                }

                KUI.Info(text);
                uiButton1.Text = "获取直链";
                linkList.Clear();

                return;
            }

            if (string.IsNullOrEmpty(uiTextBox1.Text))
            {
                KUI.Error("请填入分享链接！\nPlease enter the shared link!");
                return;
            }

            (DownloadState state, string? link) result;

            if (!string.IsNullOrEmpty(uiTextBox2.Text))
            {
                result = await KCNLanzouLinkHelper.GetDirectLinkAsync(uiTextBox1.Text, uiTextBox2.Text, 10);
            }
            else
            {
                result = await KCNLanzouLinkHelper.GetDirectLinkAsync(uiTextBox1.Text);
            }

            switch (result.state)
            {
                case DownloadState.Success:
                    KUI.OK($"直链获取成功！\nDirect link retrieved successfully!\n{result.link}");
                    break;

                case DownloadState.UrlNotProvided:
                    KUI.Error("请填入分享链接！\nPlease enter the shared link!");
                    break;

                case DownloadState.HtmlContentNotFound:
                    KUI.Error("无法获取网页内容，请检查链接是否有效！\nUnable to retrieve web content. Please check if the link is valid!");
                    break;

                case DownloadState.PostsignNotFound:
                    KUI.Error("无法解析加密信息，请检查链接或密钥是否正确！\nUnable to parse encrypted information. Please check if the link or key is correct!");
                    break;

                case DownloadState.IntermediateUrlNotFound:
                    KUI.Error("中间链接解析失败，请稍后再试！\nFailed to parse the intermediate link. Please try again later!");
                    break;

                case DownloadState.FinalUrlNotFound:
                    KUI.Error("无法获取最终直链，请检查网络连接！\nUnable to retrieve the final direct link. Please check your network connection!");
                    break;

                default:
                    KUI.Error("直链获取失败，发生未知错误！\nDirect link retrieval failed. An unknown error occurred!");
                    break;
            }
        }

        private async void uiButton2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(uiTextBox1.Text))
            {
                KUI.Error("请填入分享链接！\nPlease enter the shared link!");
                return;
            }

            (DownloadState state, LanzouFileInfo? fileInfo) result;

            if (string.IsNullOrEmpty(uiTextBox2.Text))
                result = await KCNLanzouLinkHelper.GetFileInfoAsync(uiTextBox1.Text);
            else
                result = await KCNLanzouLinkHelper.GetFileInfoAsync(uiTextBox1.Text, uiTextBox2.Text);

            if (result.state == DownloadState.Success && result.fileInfo != null)
            {
                LanzouFileInfo fileInfo = result.fileInfo;

                string message = $"文件信息解析成功：\n" +
                                 $"File info retrieved successfully:\n" +
                                 $"文件名称\\File Name: {fileInfo.FileName}\n" +
                                 $"文件大小\\File Size: {fileInfo.Size}\n" +
                                 $"上传时间\\Upload Time: {fileInfo.UploadTime}\n" +
                                 $"上传者\\Uploader: {fileInfo.Uploader}\n" +
                                 $"运行平台\\Platform: {fileInfo.Platform}\n" +
                                 $"文件描述\\Description: {fileInfo.Description}";

                KUI.OK(message);
            }
            else
            {
                switch (result.state)
                {
                    case DownloadState.HtmlContentNotFound:
                        KUI.Error("无法获取网页内容，请检查链接是否有效！\nUnable to retrieve web content. Please check the link!");
                        break;

                    default:
                        KUI.Error("解析过程中发生错误，请稍后重试！\nAn error occurred during parsing. Please try again later!");
                        break;
                }
            }
        }

        private void uiButton3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(uiTextBox1.Text))
            {
                KUI.Error("请填入分享链接！\nPlease enter the shared link!");
                return;
            }

            linkList.Add(new Tuple<string, string?>(uiTextBox1.Text, uiTextBox2.Text));
            uiButton1.Text = $"获取直链({linkList.Count})";
            uiTextBox1.Text = uiTextBox2.Text = string.Empty;
        }
    }
}
