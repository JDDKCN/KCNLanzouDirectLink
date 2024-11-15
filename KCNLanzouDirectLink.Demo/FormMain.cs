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
                KUI.Error("������������ӣ�");
                return;
            }

            string? link = null;
            if (!string.IsNullOrEmpty(uiTextBox2.Text))
            {
                for (int i = 0; i < 10; i++)
                {
                    link = await KCNLanzouLinkHelper.GetDirectLinkAsync(uiTextBox1.Text, uiTextBox2.Text); // �����Ƽ���ֱ����ȡ���ȶ����ೢ�Լ���
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
                KUI.Error("ֱ����ȡʧ�ܣ�");
                return;
            }

            KUI.OK($"ֱ����ȡ�ɹ���\n{link}");
        }
    }
}
