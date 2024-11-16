namespace KCNLanzouDirectLink.Demo
{
    public partial class FormMain : Sunny.UI.UIForm
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private async void uiButton1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(uiTextBox1.Text))
            {
                KUI.Error("������������ӣ�\nPlease enter the shared link!");
                return;
            }

            (DownloadState state, string? link) result = (DownloadState.Error, null);

            if (!string.IsNullOrEmpty(uiTextBox2.Text))
            {
                // �����Ƽ���ֱ����ȡ���ȶ����ೢ�Լ���
                for (int i = 0; i < 10; i++)
                {
                    result = await KCNLanzouLinkHelper.GetDirectLinkAsync(uiTextBox1.Text, uiTextBox2.Text);
                    if (result.state == DownloadState.Success)
                        break;
                }
            }
            else
            {
                result = await KCNLanzouLinkHelper.GetDirectLinkAsync(uiTextBox1.Text);
            }

            switch (result.state)
            {
                case DownloadState.Success:
                    KUI.OK($"ֱ����ȡ�ɹ���\nDirect link retrieved successfully!\n{result.link}");
                    break;

                case DownloadState.UrlNotProvided:
                    KUI.Error("������������ӣ�\nPlease enter the shared link!");
                    break;

                case DownloadState.HtmlContentNotFound:
                    KUI.Error("�޷���ȡ��ҳ���ݣ����������Ƿ���Ч��\nUnable to retrieve web content. Please check if the link is valid!");
                    break;

                case DownloadState.PostsignNotFound:
                    KUI.Error("�޷�����������Ϣ���������ӻ���Կ�Ƿ���ȷ��\nUnable to parse encrypted information. Please check if the link or key is correct!");
                    break;

                case DownloadState.IntermediateUrlNotFound:
                    KUI.Error("�м����ӽ���ʧ�ܣ����Ժ����ԣ�\nFailed to parse the intermediate link. Please try again later!");
                    break;

                case DownloadState.FinalUrlNotFound:
                    KUI.Error("�޷���ȡ����ֱ���������������ӣ�\nUnable to retrieve the final direct link. Please check your network connection!");
                    break;

                default:
                    KUI.Error("ֱ����ȡʧ�ܣ�����δ֪����\nDirect link retrieval failed. An unknown error occurred!");
                    break;
            }
        }

        private async void uiButton2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(uiTextBox1.Text))
            {
                KUI.Error("������������ӣ�\nPlease enter the shared link!");
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

                string message = $"�ļ���Ϣ�����ɹ���\n" +
                                 $"File info retrieved successfully:\n" +
                                 $"�ļ�����\\File Name: {fileInfo.FileName}\n" +
                                 $"�ļ���С\\File Size: {fileInfo.Size}\n" +
                                 $"�ϴ�ʱ��\\Upload Time: {fileInfo.UploadTime}\n" +
                                 $"�ϴ���\\Uploader: {fileInfo.Uploader}\n" +
                                 $"����ƽ̨\\Platform: {fileInfo.Platform}\n" +
                                 $"�ļ�����\\Description: {fileInfo.Description}";

                KUI.OK(message);
            }
            else
            {
                switch (result.state)
                {
                    case DownloadState.HtmlContentNotFound:
                        KUI.Error("�޷���ȡ��ҳ���ݣ����������Ƿ���Ч��\nUnable to retrieve web content. Please check the link!");
                        break;

                    default:
                        KUI.Error("���������з����������Ժ����ԣ�\nAn error occurred during parsing. Please try again later!");
                        break;
                }
            }
        }
    }
}
