using System;
using System.Windows;

namespace FunctionClassroom
{
    internal static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application application = new Application();
            application.ShutdownMode = ShutdownMode.OnMainWindowClose;
            try
            {
                application.Run(new MainWindow());
            }
            catch (Exception error)
            {
                MessageBox.Show(
                    "软件启动失败：\n" + error.Message,
                    "函数课堂",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
