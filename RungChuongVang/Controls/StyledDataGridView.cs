using System.Drawing;
using System.Windows.Forms;
using RungChuongVang.Helpers;

namespace RungChuongVang.Controls
{
    /// <summary>
    /// DataGridView chuẩn hóa đồng bộ giao diện toàn phần mềm: tiêu đề xanh dương đậm, chữ rõ, không tràn nội dung.
    /// </summary>
    public class StyledDataGridView : DataGridView
    {
        public StyledDataGridView()
        {
            DoubleBuffered = true;
            BackgroundColor = ThemeColors.SurfaceWhite;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            GridColor = ThemeColors.BorderLight;
            EnableHeadersVisualStyles = false;
            RowHeadersVisible = false;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            ReadOnly = true;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            RowTemplate.Height = 38;
            ColumnHeadersHeight = 42;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = ThemeColors.RoyalBlue,
                ForeColor = Color.White,
                Font = ThemeColors.FontBodyBold,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };

            DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = ThemeColors.SurfaceWhite,
                ForeColor = ThemeColors.TextPrimary,
                Font = ThemeColors.FontBody,
                SelectionBackColor = ThemeColors.SoftBlueBg,
                SelectionForeColor = ThemeColors.NavyDark,
                Padding = new Padding(10, 4, 10, 4)
            };

            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 253),
                ForeColor = ThemeColors.TextPrimary,
                Font = ThemeColors.FontBody,
                SelectionBackColor = ThemeColors.SoftBlueBg,
                SelectionForeColor = ThemeColors.NavyDark,
                Padding = new Padding(10, 4, 10, 4)
            };
        }
    }
}
