using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Visupra7
{
    internal sealed class FullscreenPreviewForm : Form
    {
        private readonly Panel previewHost = new Panel();
        private readonly Action startRecording;
        private readonly Func<Task> stopRecording;
        private readonly Func<Task> takeSnapshot;
        private FullscreenControlOverlay overlay;

        public event Action<int, int> PreviewSizeChanged;
        public IntPtr PreviewHandle { get { return previewHost.Handle; } }
        public Size PreviewSize { get { return previewHost.ClientSize; } }

        public FullscreenPreviewForm(Action onStartRecording, Func<Task> onStopRecording, Func<Task> onTakeSnapshot)
        {
            startRecording = onStartRecording; stopRecording = onStopRecording; takeSnapshot = onTakeSnapshot;
            Text = "Visupra7 · Anteprima a schermo intero"; FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black; KeyPreview = true; ShowInTaskbar = false; TopMost = true; AutoScaleMode = AutoScaleMode.Dpi;
            previewHost.Dock = DockStyle.Fill; previewHost.BackColor = Color.Black; Controls.Add(previewHost);
            KeyDown += OnFullscreenKeyDown; Resize += OnFullscreenResize;
        }

        protected override void OnShown(EventArgs e)
        {
            Bounds = Screen.FromControl(Owner ?? this).Bounds; base.OnShown(e);
            overlay = new FullscreenControlOverlay(startRecording, stopRecording, takeSnapshot, Close);
            overlay.Show(this); PositionOverlay(); previewHost.Focus();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (overlay != null && !overlay.IsDisposed) overlay.Close(); overlay = null; base.OnFormClosing(e);
        }

        private void OnFullscreenKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.F11) { e.Handled = true; Close(); }
        }

        private void OnFullscreenResize(object sender, EventArgs e)
        {
            PositionOverlay(); var handler = PreviewSizeChanged; if (handler != null) handler(previewHost.ClientSize.Width, previewHost.ClientSize.Height);
        }

        private void PositionOverlay()
        {
            if (overlay == null || overlay.IsDisposed) return;
            overlay.Location = new Point(Left + Math.Max(12, (Width - overlay.Width) / 2), Top + Math.Max(12, Height - overlay.Height - 30));
        }

        public void SetRecording(bool active, TimeSpan elapsed) { if (overlay != null && !overlay.IsDisposed) overlay.SetRecording(active, elapsed); }

        private sealed class FullscreenControlOverlay : Form
        {
            private static readonly Color Overlay = Color.FromArgb(18, 23, 31);
            private static readonly Color Border = Color.FromArgb(57, 69, 85);
            private static readonly Color TextMuted = Color.FromArgb(150, 163, 178);
            private static readonly Color Danger = Color.FromArgb(238, 72, 90);
            private static readonly Color Surface = Color.FromArgb(38, 47, 59);
            private readonly OverlayButton record = new OverlayButton();
            private readonly OverlayButton stop = new OverlayButton();
            private readonly OverlayButton snapshot = new OverlayButton();
            private readonly OverlayButton exit = new OverlayButton();
            private readonly Label recordingState = new Label();
            private readonly Action closeFullscreen;

            public FullscreenControlOverlay(Action onStartRecording, Func<Task> onStopRecording, Func<Task> onTakeSnapshot, Action onCloseFullscreen)
            {
                closeFullscreen = onCloseFullscreen; FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual; ShowInTaskbar = false;
                TopMost = true; KeyPreview = true; BackColor = Overlay; ClientSize = new Size(730, 88); AutoScaleMode = AutoScaleMode.Dpi;
                record.Text = Localization.T("OverlayRecord"); record.BaseColor = Danger; record.HoverColor = Color.FromArgb(250, 88, 105); record.Location = new Point(16, 18); record.Size = new Size(112, 50);
                stop.Text = Localization.T("OverlayStop"); stop.BaseColor = Surface; stop.HoverColor = Color.FromArgb(55, 66, 81); stop.Location = new Point(138, 18); stop.Size = new Size(112, 50);
                snapshot.Text = Localization.T("OverlayCapture"); snapshot.BaseColor = Surface; snapshot.HoverColor = Color.FromArgb(55, 66, 81); snapshot.Location = new Point(260, 18); snapshot.Size = new Size(190, 50);
                recordingState.Text = Localization.T("OverlayReady"); recordingState.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); recordingState.ForeColor = TextMuted;
                recordingState.TextAlign = ContentAlignment.MiddleCenter; recordingState.Location = new Point(460, 18); recordingState.Size = new Size(140, 50);
                exit.Text = Localization.T("OverlayExit"); exit.BaseColor = Color.FromArgb(31, 38, 48); exit.HoverColor = Color.FromArgb(50, 59, 72); exit.Location = new Point(610, 18); exit.Size = new Size(104, 50);
                Controls.Add(record); Controls.Add(stop); Controls.Add(snapshot); Controls.Add(recordingState); Controls.Add(exit);
                record.Click += delegate { record.Enabled = false; onStartRecording(); }; stop.Click += async delegate { stop.Enabled = false; await onStopRecording(); }; snapshot.Click += async delegate { snapshot.Enabled = false; try { await onTakeSnapshot(); } finally { snapshot.Enabled = true; } }; exit.Click += delegate { closeFullscreen(); };
                KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.F11) { e.Handled = true; closeFullscreen(); } };
                Paint += delegate(object sender, PaintEventArgs e) { using (var pen = new Pen(Border)) e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1); };
                SetRecording(false, TimeSpan.Zero);
            }

            public void SetRecording(bool active, TimeSpan elapsed)
            {
                record.Enabled = !active; stop.Enabled = active; recordingState.Text = active ? "●  REC  " + elapsed.ToString(@"hh\:mm\:ss") : Localization.T("OverlayReady"); recordingState.ForeColor = active ? Danger : TextMuted;
            }
        }

        private sealed class OverlayButton : Button
        {
            private bool hovering, pressed;
            public Color BaseColor { get; set; } public Color HoverColor { get; set; }
            public OverlayButton()
            {
                ForeColor = Color.White; Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }
            protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hovering = pressed = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
            protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; Color fill = !Enabled ? Color.FromArgb(29, 35, 43) : pressed ? ControlPaint.Dark(HoverColor, .08F) : hovering ? HoverColor : BaseColor;
                Rectangle r = ClientRectangle; r.Width--; r.Height--; using (GraphicsPath path = RoundedRect(r, 7)) using (var brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? ForeColor : Color.FromArgb(82, 92, 104), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
            private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
            {
                int d = radius * 2; var path = new GraphicsPath(); path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90); path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
                path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90); path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
            }
        }
    }
}
