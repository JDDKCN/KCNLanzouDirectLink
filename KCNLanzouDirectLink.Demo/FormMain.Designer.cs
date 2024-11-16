namespace KCNLanzouDirectLink.Demo
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            uiButton1 = new Sunny.UI.UIButton();
            uiTextBox1 = new Sunny.UI.UITextBox();
            uiTextBox2 = new Sunny.UI.UITextBox();
            uiLabel1 = new Sunny.UI.UILabel();
            uiLabel2 = new Sunny.UI.UILabel();
            uiButton2 = new Sunny.UI.UIButton();
            SuspendLayout();
            // 
            // uiButton1
            // 
            uiButton1.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            uiButton1.Location = new Point(117, 299);
            uiButton1.MinimumSize = new Size(1, 1);
            uiButton1.Name = "uiButton1";
            uiButton1.Size = new Size(356, 75);
            uiButton1.TabIndex = 0;
            uiButton1.Text = "获取直链";
            uiButton1.TipsFont = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            uiButton1.Click += uiButton1_Click;
            // 
            // uiTextBox1
            // 
            uiTextBox1.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            uiTextBox1.Location = new Point(117, 153);
            uiTextBox1.Margin = new Padding(4, 5, 4, 5);
            uiTextBox1.MinimumSize = new Size(1, 16);
            uiTextBox1.Name = "uiTextBox1";
            uiTextBox1.Padding = new Padding(5);
            uiTextBox1.ShowText = false;
            uiTextBox1.Size = new Size(561, 64);
            uiTextBox1.TabIndex = 1;
            uiTextBox1.TextAlignment = ContentAlignment.MiddleLeft;
            uiTextBox1.Watermark = "蓝奏云分享链接";
            // 
            // uiTextBox2
            // 
            uiTextBox2.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            uiTextBox2.Location = new Point(117, 227);
            uiTextBox2.Margin = new Padding(4, 5, 4, 5);
            uiTextBox2.MinimumSize = new Size(1, 16);
            uiTextBox2.Name = "uiTextBox2";
            uiTextBox2.Padding = new Padding(5);
            uiTextBox2.ShowText = false;
            uiTextBox2.Size = new Size(561, 64);
            uiTextBox2.TabIndex = 2;
            uiTextBox2.TextAlignment = ContentAlignment.MiddleLeft;
            uiTextBox2.Watermark = "文件分享密码(可选)";
            // 
            // uiLabel1
            // 
            uiLabel1.BackColor = Color.Transparent;
            uiLabel1.Dock = DockStyle.Bottom;
            uiLabel1.Font = new Font("微软雅黑", 10F);
            uiLabel1.ForeColor = Color.FromArgb(48, 48, 48);
            uiLabel1.Location = new Point(0, 402);
            uiLabel1.Name = "uiLabel1";
            uiLabel1.Size = new Size(800, 48);
            uiLabel1.TabIndex = 3;
            uiLabel1.Text = "Copyright ©2023-2024 剧毒的KCN, All Rights Reserved.";
            uiLabel1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // uiLabel2
            // 
            uiLabel2.Font = new Font("微软雅黑", 16F);
            uiLabel2.ForeColor = Color.FromArgb(48, 48, 48);
            uiLabel2.Location = new Point(117, 65);
            uiLabel2.Name = "uiLabel2";
            uiLabel2.Size = new Size(561, 74);
            uiLabel2.TabIndex = 4;
            uiLabel2.Text = "蓝奏云直链获取Demo";
            uiLabel2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // uiButton2
            // 
            uiButton2.FillColor = Color.FromArgb(0, 150, 136);
            uiButton2.FillColor2 = Color.FromArgb(0, 150, 136);
            uiButton2.FillHoverColor = Color.FromArgb(51, 171, 160);
            uiButton2.FillPressColor = Color.FromArgb(0, 120, 109);
            uiButton2.FillSelectedColor = Color.FromArgb(0, 120, 109);
            uiButton2.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            uiButton2.LightColor = Color.FromArgb(238, 248, 248);
            uiButton2.Location = new Point(479, 299);
            uiButton2.MinimumSize = new Size(1, 1);
            uiButton2.Name = "uiButton2";
            uiButton2.RectColor = Color.FromArgb(0, 150, 136);
            uiButton2.RectHoverColor = Color.FromArgb(51, 171, 160);
            uiButton2.RectPressColor = Color.FromArgb(0, 120, 109);
            uiButton2.RectSelectedColor = Color.FromArgb(0, 120, 109);
            uiButton2.Size = new Size(199, 75);
            uiButton2.Style = Sunny.UI.UIStyle.Custom;
            uiButton2.TabIndex = 5;
            uiButton2.Text = "获取信息";
            uiButton2.TipsFont = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            uiButton2.Click += uiButton2_Click;
            // 
            // FormMain
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(800, 450);
            Controls.Add(uiButton2);
            Controls.Add(uiLabel2);
            Controls.Add(uiLabel1);
            Controls.Add(uiTextBox2);
            Controls.Add(uiTextBox1);
            Controls.Add(uiButton1);
            Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            MaximizeBox = false;
            Name = "FormMain";
            Text = "KCNLanzouDirectLink.Demo";
            TitleFont = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            ZoomScaleRect = new Rectangle(22, 22, 800, 450);
            ResumeLayout(false);
        }

        #endregion

        private Sunny.UI.UIButton uiButton1;
        private Sunny.UI.UITextBox uiTextBox1;
        private Sunny.UI.UITextBox uiTextBox2;
        private Sunny.UI.UILabel uiLabel1;
        private Sunny.UI.UILabel uiLabel2;
        private Sunny.UI.UIButton uiButton2;
    }
}
