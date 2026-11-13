using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.Services;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Màn hình trình chiếu lớn dành cho thí sinh và khán giả (Screen 2).
    /// Không chứa menu quản trị hay nút điều khiển; tự động đồng bộ qua ExamStateManager.
    /// </summary>
    public class FrmTrinhChieu : Form
    {
        private readonly PresentationStageControl _stage;

        public FrmTrinhChieu(ExamStateManager stateManager)
        {
            Text = "Rung Chuông Vàng - Trình chiếu";
            BackColor = Helpers.ThemeColors.NavyDark;
            KeyPreview = true;
            ShowInTaskbar = true;

            _stage = new PresentationStageControl { Dock = DockStyle.Fill, EnableAnimation = true };
            _stage.BindState(stateManager);
            Controls.Add(_stage);

            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape && FormBorderStyle == FormBorderStyle.None)
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = FormWindowState.Normal;
                    Size = new System.Drawing.Size(960, 540);
                }
                else if (e.KeyCode == Keys.F11)
                {
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                }
            };
        }
    }
}
