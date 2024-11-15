using Sunny.UI;

namespace KCNLanzouDirectLink.Demo
{
    internal class KUI
    {
        public static void Error(string msg)
        {
            Sunny.UI.UIForm form = new Sunny.UI.UIForm();
            form.ShowErrorDialog("Error", msg);
        }

        public static void Error(string msg, string title)
        {
            Sunny.UI.UIForm form = new Sunny.UI.UIForm();
            form.ShowErrorDialog(title, msg);
        }

        public static void Warning(string msg)
        {
            Sunny.UI.UIForm form = new Sunny.UI.UIForm();
            form.ShowWarningDialog("Warn", msg);
        }

        public static void Warning(string msg, string title)
        {
            Sunny.UI.UIForm form = new Sunny.UI.UIForm();
            form.ShowWarningDialog(title, msg);
        }

        public static void OK(string msg)
        {
            Sunny.UI.UIForm form = new Sunny.UI.UIForm();
            form.ShowSuccessDialog("OK", msg);
        }

        public static void OK(string msg, string title)
        {
            Sunny.UI.UIForm form = new Sunny.UI.UIForm();
            form.ShowSuccessDialog(title, msg);
        }

        public static void Info(string msg)
        {
            Sunny.UI.UIForm form = new Sunny.UI.UIForm();
            form.ShowInfoDialog("Info", msg, UIStyle.Blue);
        }

        public static void Info(string msg, string title)
        {
            Sunny.UI.UIForm form = new Sunny.UI.UIForm();
            form.ShowInfoDialog(title, msg, UIStyle.Blue);
        }
    }
}
