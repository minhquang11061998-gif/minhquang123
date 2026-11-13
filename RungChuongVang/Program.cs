using System;
using System.Threading;
using System.Windows.Forms;
using RungChuongVang.DataAccess;
using RungChuongVang.Forms;
using RungChuongVang.Helpers;

namespace RungChuongVang
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            try
            {
                FileHelper.EnsureApplicationDirectories();

                if (!DatabaseHelper.IsOleDbProviderInstalled(out string providerError))
                {
                    MessageBox.Show(
                        providerError,
                        "Thiếu trình điều khiển Microsoft Access",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                DatabaseInitializer.EnsureDatabaseInitialized();
                new Services.BackupService().TryAutoBackupIfEnabled();

                Application.Run(new FrmMain());
            }
            catch (Exception ex)
            {
                FileHelper.WriteErrorLog("Program.Main", ex);
                MessageBox.Show(
                    $"Không thể khởi động Phần mềm Rung Chuông Vàng:\n{ex.Message}",
                    "Lỗi khởi động",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            FileHelper.WriteErrorLog("UI.ThreadException", e.Exception);
            MessageBox.Show(
                $"Đã xảy ra lỗi trong quá trình thao tác:\n{e.Exception.Message}",
                "Thông báo lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                FileHelper.WriteErrorLog("AppDomain.UnhandledException", ex);
                MessageBox.Show(
                    $"Lỗi hệ thống không mong muốn:\n{ex.Message}",
                    "Lỗi nghiêm trọng",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
