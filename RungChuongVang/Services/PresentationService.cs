using System;
using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Forms;

namespace RungChuongVang.Services
{
    /// <summary>
    /// Quản lý phát hiện màn hình (Screen.AllScreens), gán màn hình trình chiếu, đảo màn hình và cập nhật UI an toàn luồng.
    /// </summary>
    public sealed class PresentationService : IDisposable
    {
        private FrmTrinhChieu? _presentationForm;
        private int _currentScreenIndex = -1;
        private bool _isFullScreen = true;

        public bool IsPresentationOpen => _presentationForm != null && !_presentationForm.IsDisposed;

        public Screen[] GetAllScreens() => Screen.AllScreens;

        public int GetDefaultPresentationScreenIndex()
        {
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                if (!screens[i].Primary) return i;
            }
            return 0;
        }

        public void OpenOrUpdatePresentation(ExamStateManager stateManager, int screenIndex = -1, bool? forceWindowedTest = null)
        {
            Screen[] screens = Screen.AllScreens;
            if (screenIndex < 0 || screenIndex >= screens.Length)
            {
                screenIndex = GetDefaultPresentationScreenIndex();
            }

            bool singleMonitor = screens.Length == 1;
            bool fullScreen = forceWindowedTest.HasValue ? !forceWindowedTest.Value : !singleMonitor;

            _currentScreenIndex = screenIndex;
            _isFullScreen = fullScreen;

            if (!IsPresentationOpen)
            {
                _presentationForm = new FrmTrinhChieu(stateManager);
                _presentationForm.FormClosed += (_, _) => _presentationForm = null;
                ApplyScreenPlacement(_presentationForm, screens[screenIndex], fullScreen);
                _presentationForm.Show();
            }
            else
            {
                ApplyScreenPlacement(_presentationForm!, screens[screenIndex], fullScreen);
                _presentationForm!.BringToFront();
            }
        }

        public void SwapScreens(Form controlForm, ExamStateManager stateManager)
        {
            Screen[] screens = Screen.AllScreens;
            if (screens.Length < 2)
            {
                MessageBox.Show("Máy tính hiện chỉ phát hiện 1 màn hình. Chế độ chạy thử cửa sổ đang được hỗ trợ.", "Thông báo màn hình", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Screen currentControlScreen = Screen.FromControl(controlForm);
            int controlIdx = Array.IndexOf(screens, currentControlScreen);
            if (controlIdx < 0) controlIdx = 0;

            int presIdx = _currentScreenIndex >= 0 && _currentScreenIndex < screens.Length
                ? _currentScreenIndex
                : GetDefaultPresentationScreenIndex();

            if (presIdx == controlIdx)
            {
                presIdx = (controlIdx + 1) % screens.Length;
            }

            // Đảo vị trí: Control sang màn hình Presentation cũ, Presentation sang màn hình Control cũ
            FormWindowState prevState = controlForm.WindowState;
            controlForm.WindowState = FormWindowState.Normal;
            controlForm.Bounds = screens[presIdx].WorkingArea;
            if (prevState == FormWindowState.Maximized)
            {
                controlForm.WindowState = FormWindowState.Maximized;
            }

            OpenOrUpdatePresentation(stateManager, controlIdx, forceWindowedTest: false);
        }

        public void ClosePresentation()
        {
            if (IsPresentationOpen)
            {
                _presentationForm!.Close();
                _presentationForm.Dispose();
                _presentationForm = null;
            }
        }

        private static void ApplyScreenPlacement(Form form, Screen targetScreen, bool fullScreen)
        {
            if (fullScreen)
            {
                form.WindowState = FormWindowState.Normal;
                form.FormBorderStyle = FormBorderStyle.None;
                form.StartPosition = FormStartPosition.Manual;
                form.Bounds = targetScreen.Bounds;
                form.WindowState = FormWindowState.Maximized;
            }
            else
            {
                form.WindowState = FormWindowState.Normal;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.StartPosition = FormStartPosition.Manual;
                int w = Math.Min(960, targetScreen.WorkingArea.Width - 60);
                int h = Math.Min(540, targetScreen.WorkingArea.Height - 60);
                form.Bounds = new Rectangle(
                    targetScreen.WorkingArea.Right - w - 20,
                    targetScreen.WorkingArea.Bottom - h - 20,
                    w, h);
            }
        }

        /// <summary>
        /// Bảo đảm cập nhật giao diện an toàn giữa các luồng (Invoke/BeginInvoke).
        /// </summary>
        public static void RunOnUi(Control target, Action action)
        {
            if (target.IsDisposed || !target.IsHandleCreated) return;
            if (target.InvokeRequired)
            {
                try { target.BeginInvoke(action); } catch { }
            }
            else
            {
                action();
            }
        }

        public void Dispose()
        {
            ClosePresentation();
        }
    }
}
