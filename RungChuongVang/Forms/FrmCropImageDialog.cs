using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using RungChuongVang.Controls;
using RungChuongVang.Helpers;

namespace RungChuongVang.Forms
{
    /// <summary>
    /// Hộp thoại cắt ảnh, điều chỉnh vị trí (kéo thả) và phóng to/thu nhỏ ảnh người chiến thắng (giữ nguyên tỷ lệ).
    /// Lưu kết quả vào Data/Images/Winners/ và trả về đường dẫn tương đối.
    /// </summary>
    public class FrmCropImageDialog : Form
    {
        private readonly Image _sourceImage;
        private readonly Panel _canvas;
        private readonly TrackBar _zoomTrack;
        private readonly ComboBox _cboAspect;

        private float _zoom = 1.0f;
        private PointF _offset = new(0, 0);
        private bool _dragging;
        private Point _lastMouse;
        private Size _outputSize = new(600, 750);

        public string SavedRelativePath { get; private set; } = string.Empty;

        public FrmCropImageDialog(string sourceFilePath)
        {
            _sourceImage = ImageHelper.LoadImageSafe(sourceFilePath)
                ?? throw new InvalidOperationException("Không thể đọc tập tin ảnh đã chọn.");

            Text = "Cắt và điều chỉnh ảnh vinh danh";
            Size = new Size(720, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ThemeColors.AppBackground;
            Font = ThemeColors.FontBody;

            var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(14) };
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));

            var pnlTop = new Panel { Dock = DockStyle.Fill };
            pnlTop.Controls.Add(new Label { Text = "Khung hình:", Location = new Point(0, 12), AutoSize = true, Font = ThemeColors.FontBodyBold });
            _cboAspect = new ComboBox { Location = new Point(92, 8), Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboAspect.Items.AddRange(new object[]
            {
                "Chân dung đứng (3:4)",
                "Toàn thân / Vuông (1:1)",
                "Ảnh tập thể / Đội (4:3)",
                "Ảnh sân khấu rộng (16:9)"
            });
            _cboAspect.SelectedIndex = 0;
            _cboAspect.SelectedIndexChanged += (_, _) => UpdateAspect();

            var btnCenter = new ModernButton { Text = "Căn giữa ảnh", Location = new Point(338, 6), Size = new Size(130, 34), NormalBackColor = ThemeColors.RoyalBlue, HoverBackColor = ThemeColors.NavyDark };
            btnCenter.Click += (_, _) => FitAndCenter();
            pnlTop.Controls.AddRange(new Control[] { _cboAspect, btnCenter });

            _canvas = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = ThemeColors.NavyDark, Cursor = Cursors.SizeAll };
            _canvas.Paint += OnCanvasPaint;
            _canvas.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _dragging = true; _lastMouse = e.Location; } };
            _canvas.MouseUp += (_, _) => _dragging = false;
            _canvas.MouseMove += (_, e) =>
            {
                if (_dragging)
                {
                    _offset = new PointF(_offset.X + (e.X - _lastMouse.X), _offset.Y + (e.Y - _lastMouse.Y));
                    _lastMouse = e.Location;
                    _canvas.Invalidate();
                }
            };

            var pnlBottom = new Panel { Dock = DockStyle.Fill };
            pnlBottom.Controls.Add(new Label { Text = "Thu phóng:", Location = new Point(0, 10), AutoSize = true, Font = ThemeColors.FontBodyBold });
            _zoomTrack = new TrackBar { Location = new Point(85, 2), Width = 320, Minimum = 20, Maximum = 300, Value = 100, TickFrequency = 20 };
            _zoomTrack.Scroll += (_, _) => { _zoom = _zoomTrack.Value / 100f; _canvas.Invalidate(); };

            var btnSave = new ModernButton { Text = "Lưu ảnh đã chỉnh", Location = new Point(435, 16), Size = new Size(155, 38), NormalBackColor = ThemeColors.SuccessGreen, HoverBackColor = Color.FromArgb(16, 130, 58) };
            btnSave.Click += (_, _) => SaveCroppedImage();
            var btnCancel = new ModernButton { Text = "Hủy", Location = new Point(598, 16), Size = new Size(85, 38), NormalBackColor = ThemeColors.TextMuted, HoverBackColor = ThemeColors.TextPrimary };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            pnlBottom.Controls.AddRange(new Control[] { _zoomTrack, btnSave, btnCancel });

            tlp.Controls.Add(pnlTop, 0, 0);
            tlp.Controls.Add(_canvas, 0, 1);
            tlp.Controls.Add(pnlBottom, 0, 2);
            Controls.Add(tlp);

            Shown += (_, _) => FitAndCenter();
        }

        private void UpdateAspect()
        {
            _outputSize = _cboAspect.SelectedIndex switch
            {
                0 => new Size(600, 800),
                1 => new Size(700, 700),
                2 => new Size(800, 600),
                _ => new Size(960, 540)
            };
            FitAndCenter();
        }

        private Rectangle GetCropFrameRect()
        {
            float pad = 24f;
            float availW = Math.Max(100, _canvas.Width - pad * 2);
            float availH = Math.Max(100, _canvas.Height - pad * 2);
            float ratio = Math.Min(availW / _outputSize.Width, availH / _outputSize.Height);
            int fw = (int)(_outputSize.Width * ratio);
            int fh = (int)(_outputSize.Height * ratio);
            return new Rectangle((_canvas.Width - fw) / 2, (_canvas.Height - fh) / 2, fw, fh);
        }

        private void FitAndCenter()
        {
            Rectangle frame = GetCropFrameRect();
            float baseScale = Math.Max((float)frame.Width / _sourceImage.Width, (float)frame.Height / _sourceImage.Height);
            _zoom = Math.Clamp(baseScale, 0.2f, 3.0f);
            _zoomTrack.Value = Math.Clamp((int)Math.Round(_zoom * 100), _zoomTrack.Minimum, _zoomTrack.Maximum);
            _offset = new PointF(0, 0);
            _canvas.Invalidate();
        }

        private void OnCanvasPaint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle frame = GetCropFrameRect();
            float drawW = _sourceImage.Width * _zoom;
            float drawH = _sourceImage.Height * _zoom;
            float drawX = frame.X + (frame.Width - drawW) / 2f + _offset.X;
            float drawY = frame.Y + (frame.Height - drawH) / 2f + _offset.Y;

            g.DrawImage(_sourceImage, drawX, drawY, drawW, drawH);

            // Phủ tối vùng ngoài khung cắt
            using var maskRegion = new Region(_canvas.ClientRectangle);
            maskRegion.Exclude(frame);
            using var dimBrush = new SolidBrush(Color.FromArgb(150, 5, 12, 26));
            g.FillRegion(dimBrush, maskRegion);

            using var goldPen = new Pen(ThemeColors.GoldBright, 2.5f);
            g.DrawRectangle(goldPen, frame);
        }

        private void SaveCroppedImage()
        {
            Rectangle frame = GetCropFrameRect();
            float drawW = _sourceImage.Width * _zoom;
            float drawH = _sourceImage.Height * _zoom;
            float drawX = frame.X + (frame.Width - drawW) / 2f + _offset.X;
            float drawY = frame.Y + (frame.Height - drawH) / 2f + _offset.Y;

            float scaleToOut = (float)_outputSize.Width / frame.Width;

            using var bmp = new Bitmap(_outputSize.Width, _outputSize.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(ThemeColors.NavyDark);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(
                    _sourceImage,
                    (drawX - frame.X) * scaleToOut,
                    (drawY - frame.Y) * scaleToOut,
                    drawW * scaleToOut,
                    drawH * scaleToOut);
            }

            FileHelper.EnsureApplicationDirectories();
            string fileName = $"WIN_{DateTime.Now:yyyyMMddHHmmssfff}.png";
            string destPath = Path.Combine(FileHelper.WinnerImagesDirectory, fileName);
            bmp.Save(destPath, ImageFormat.Png);

            SavedRelativePath = FileHelper.ToRelativeDataPath(destPath);
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sourceImage.Dispose();
            }
            base.Dispose(disposing);
        }

        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel() => DoubleBuffered = true;
        }
    }
}
