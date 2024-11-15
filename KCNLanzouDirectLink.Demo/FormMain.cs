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
                KUI.Error("请填入分享链接！");
                return;
            }

            string? link = null;
            if (!string.IsNullOrEmpty(uiTextBox2.Text))
            {
                for (int i = 0; i < 10; i++)
                {
                    link = await KCNLanzouLinkHelper.GetDirectLinkAsync(uiTextBox1.Text, uiTextBox2.Text); // 蓝奏云加密直链获取不稳定，多尝试几次
                    if (!string.IsNullOrEmpty(link))
                        break;
                }
            }
            else
            {
                link = await KCNLanzouLinkHelper.GetDirectLinkAsync(uiTextBox1.Text);
            }

            if (link == null)
            {
                KUI.Error("直链获取失败！");
                return;
            }

            KUI.OK($"直链获取成功！\n{link}");
        }
    }
}
