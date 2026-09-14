using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Visupra7
{
    internal sealed class MainForm : Form
    {
        private static readonly Color WindowBack = Color.FromArgb(12, 16, 22);
        private static readonly Color Surface = Color.FromArgb(22, 28, 37);
        private static readonly Color SurfaceLight = Color.FromArgb(29, 37, 48);
        private static readonly Color Border = Color.FromArgb(45, 56, 70);
        private static readonly Color TextMain = Color.FromArgb(239, 244, 250);
        private static readonly Color TextMuted = Color.FromArgb(145, 158, 174);
        private static readonly Color Accent = Color.FromArgb(50, 145, 255);
        private static readonly Color AccentHover = Color.FromArgb(74, 160, 255);
        private static readonly Color Success = Color.FromArgb(48, 201, 138);
        private static readonly Color Danger = Color.FromArgb(238, 72, 90);

        private readonly AppSettings settings;
        private readonly Logger log;
        private readonly WebcamCapture capture;
        private readonly FfmpegRecorder recorder;
        private readonly ComboBox devices = new ComboBox();
        private readonly ComboBox formats = new ComboBox();
        private readonly ModernButton detect = new ModernButton();
        private readonly ModernButton start = new ModernButton();
        private readonly ModernButton stop = new ModernButton();
        private readonly ModernButton screenshot = new ModernButton();
        private readonly ModernButton record = new ModernButton();
        private readonly ModernButton stopRecord = new ModernButton();
        private readonly ModernButton toggleLog = new ModernButton();
        private readonly ModernButton fullscreen = new ModernButton();
        private readonly ComboBox language = new ComboBox();
        private readonly Panel preview = new Panel();
        private readonly Label previewPlaceholder = new Label();
        private readonly TextBox logBox = new TextBox();
        private readonly Label state = new Label();
        private readonly Label connectionState = new Label();
        private readonly Label rec = new Label();
        private readonly Label elapsed = new Label();
        private readonly Label formatInfo = new Label();
        private readonly CardPanel logCard = new CardPanel();
        private readonly Timer timer = new Timer();
        private readonly ToolTip tips = new ToolTip();
        private List<WebcamDevice> deviceList = new List<WebcamDevice>();
        private bool closing;
        private bool logExpanded = false;
        private FullscreenPreviewForm fullscreenWindow;

        public MainForm(AppSettings appSettings, Logger logger)
        {
            settings = appSettings;
            log = logger;
            capture = new WebcamCapture(log);
            recorder = new FfmpegRecorder(settings, log);
            capture.FrameArrived += recorder.Enqueue;
            recorder.Failed += RecorderFailed;

            Text = "Visupra7";
            MinimumSize = new Size(800, 600);
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = WindowBack;
            ForeColor = TextMain;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            ShowIcon = false;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            BuildUi();
            ApplyTranslations();
            tips.InitialDelay = 350; tips.ReshowDelay = 100; tips.AutoPopDelay = 8000;
            WireEvents();
            log.LineWritten += AppendLog;
            timer.Interval = 250;
            timer.Tick += TimerTick;
            timer.Start();
            UpdateButtons();
            Shown += delegate { DetectDevices(); };
        }

        private void BuildUi()
        {
            SuspendLayout();
            Controls.Add(BuildWorkspace());
            Controls.Add(BuildStatusBar());
            Controls.Add(BuildHeader());
            ResumeLayout(true);
        }

        private Control BuildHeader()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Color.FromArgb(17, 22, 29), Padding = new Padding(14, 0, 12, 0) };
            var mark = new LogoMark { Location = new Point(14, 15), Size = new Size(40, 40) };
            var title = MakeLabel("VISUPRA7", 16F, FontStyle.Bold, TextMain); title.Location = new Point(64, 12); title.AutoSize = true;
            var subtitle = MakeLabel("", 7F, FontStyle.Bold, TextMuted); subtitle.Location = new Point(66, 41); subtitle.AutoSize = true; Localization.Bind(subtitle, "AppSubtitle");

            ConfigureToolbarButton(start, ButtonIcon.Play, Accent, AccentHover, "Start preview");
            ConfigureToolbarButton(record, ButtonIcon.Record, Danger, Color.FromArgb(250, 88, 105), "Start recording");
            ConfigureToolbarButton(stop, ButtonIcon.Stop, SurfaceLight, Color.FromArgb(49, 61, 76), "Stop");
            ConfigureToolbarButton(screenshot, ButtonIcon.Capture, SurfaceLight, Color.FromArgb(49, 61, 76), "Capture image");
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 192, Padding = new Padding(4, 14, 0, 0), BackColor = Color.Transparent, WrapContents = false };
            toolbar.Controls.Add(start); toolbar.Controls.Add(record); toolbar.Controls.Add(stop); toolbar.Controls.Add(screenshot);

            elapsed.Text = "00:00:00"; elapsed.Font = new Font("Consolas", 12F, FontStyle.Bold); elapsed.ForeColor = TextMain;
            elapsed.TextAlign = ContentAlignment.MiddleCenter; elapsed.Dock = DockStyle.Right; elapsed.Width = 88;
            rec.Text = "REC"; rec.Font = new Font("Segoe UI", 8F, FontStyle.Bold); rec.ForeColor = Color.White; rec.BackColor = Danger;
            rec.TextAlign = ContentAlignment.MiddleCenter; rec.Dock = DockStyle.Right; rec.Width = 48; rec.Visible = false;
            connectionState.Text = "OFFLINE"; connectionState.Font = new Font("Segoe UI", 7F, FontStyle.Bold); connectionState.ForeColor = TextMuted;
            connectionState.TextAlign = ContentAlignment.MiddleCenter; connectionState.Dock = DockStyle.Right; connectionState.Width = 66;
            var languagePanel = new Panel { Dock = DockStyle.Right, Width = 106, BackColor = Color.Transparent };
            var languageLabel = MakeLabel("", 6.5F, FontStyle.Bold, TextMuted); languageLabel.Location = new Point(5, 8); languageLabel.AutoSize = true; Localization.Bind(languageLabel, "Language");
            ConfigureCombo(language); language.Location = new Point(4, 27); language.Size = new Size(98, 25); language.Items.Add("English"); language.Items.Add("Italiano"); language.SelectedIndex = Localization.CurrentCode == "it" ? 1 : 0;
            languagePanel.Controls.Add(languageLabel); languagePanel.Controls.Add(language);

            header.Paint += delegate(object sender, PaintEventArgs e) { using (var pen = new Pen(Border)) e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1); };
            header.Controls.Add(toolbar); header.Controls.Add(rec); header.Controls.Add(elapsed); header.Controls.Add(connectionState); header.Controls.Add(languagePanel); header.Controls.Add(mark); header.Controls.Add(title); header.Controls.Add(subtitle);
            return header;
        }

        private static void ConfigureToolbarButton(ModernButton button, ButtonIcon icon, Color baseColor, Color hoverColor, string accessibleName)
        {
            button.Text = ""; button.Icon = icon; button.Size = new Size(40, 40); button.Margin = new Padding(3, 0, 3, 0);
            button.BaseColor = baseColor; button.HoverColor = hoverColor; button.AccessibleName = accessibleName;
        }
        private Control BuildWorkspace()
        {
            var work = new Panel { Dock = DockStyle.Fill, BackColor = WindowBack, Padding = new Padding(12, 10, 12, 10) };
            logCard.Dock = DockStyle.Bottom; logCard.Height = 30; logCard.Padding = new Padding(1); logCard.BackColor = Surface;
            logCard.Controls.Add(BuildLogBody()); logCard.Controls.Add(BuildLogHeader());
            var logGap = new Panel { Dock = DockStyle.Bottom, Height = 8, BackColor = WindowBack };
            var sourceGap = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = WindowBack };
            work.Controls.Add(BuildPreviewCard()); work.Controls.Add(logGap); work.Controls.Add(logCard); work.Controls.Add(sourceGap); work.Controls.Add(BuildSourceCard());
            return work;
        }
        private Control BuildSourceCard()
        {
            var card = new CardPanel { Dock = DockStyle.Top, Width = 760, Height = 74, BackColor = Surface, Padding = new Padding(12) };
            var heading = MakeLabel("VIDEO SOURCE", 8F, FontStyle.Bold, TextMain); heading.Location = new Point(14, 12); heading.AutoSize = true;
            var cameraLabel = MakeLabel("Webcam", 7F, FontStyle.Regular, TextMuted); cameraLabel.Location = new Point(112, 8); cameraLabel.AutoSize = true;
            ConfigureCombo(devices); devices.Location = new Point(112, 27); devices.Size = new Size(210, 27);
            detect.Text = "DETECT"; detect.Location = new Point(332, 26); detect.Size = new Size(112, 29); detect.BaseColor = SurfaceLight; detect.HoverColor = Color.FromArgb(39, 50, 64);
            var formatLabel = MakeLabel("Video format", 7F, FontStyle.Regular, TextMuted); formatLabel.Location = new Point(456, 8); formatLabel.AutoSize = true;
            ConfigureCombo(formats); formats.Location = new Point(456, 27); formats.Size = new Size(205, 27); formats.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Localization.Bind(heading, "SourceVideo"); Localization.Bind(cameraLabel, "Webcam"); Localization.Bind(detect, "DetectWebcam"); Localization.Bind(formatLabel, "VideoFormat");
            card.Controls.Add(heading); card.Controls.Add(cameraLabel); card.Controls.Add(devices); card.Controls.Add(detect); card.Controls.Add(formatLabel); card.Controls.Add(formats);
            return card;
        }
        private Control BuildActionsCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(18) };
            var heading = MakeLabel("CONTROLLI", 9F, FontStyle.Bold, TextMain); heading.Location = new Point(18, 17); heading.AutoSize = true;
            start.Text = "▶   AVVIA ANTEPRIMA"; start.Location = new Point(18, 48); start.Size = new Size(238, 42); start.BaseColor = Accent; start.HoverColor = AccentHover;
            stop.Text = "■   FERMA"; stop.Location = new Point(18, 98); stop.Size = new Size(114, 38); stop.BaseColor = SurfaceLight; stop.HoverColor = Color.FromArgb(42, 52, 66);
            screenshot.Text = "SCATTA FOTO"; screenshot.Location = new Point(142, 98); screenshot.Size = new Size(114, 38); screenshot.BaseColor = SurfaceLight; screenshot.HoverColor = Color.FromArgb(42, 52, 66);
            record.Text = "●   AVVIA REGISTRAZIONE"; record.Location = new Point(18, 149); record.Size = new Size(238, 42); record.BaseColor = Danger; record.HoverColor = Color.FromArgb(250, 88, 105);
            stopRecord.Text = "■   TERMINA E SALVA MP4"; stopRecord.Location = new Point(18, 199); stopRecord.Size = new Size(238, 40); stopRecord.BaseColor = Color.FromArgb(68, 44, 50); stopRecord.HoverColor = Color.FromArgb(88, 49, 58);
            var hint = MakeLabel("L'anteprima resta attiva anche se\nl'encoder incontra un errore.", 8F, FontStyle.Regular, TextMuted); hint.Location = new Point(18, 254); hint.AutoSize = true;
            Localization.Bind(heading, "Controls"); Localization.Bind(start, "StartPreview"); Localization.Bind(stop, "Stop"); Localization.Bind(screenshot, "TakePhoto"); Localization.Bind(record, "StartRecording"); Localization.Bind(stopRecord, "StopAndSave"); Localization.Bind(hint, "StabilityHint");
            card.Controls.Add(heading); card.Controls.Add(start); card.Controls.Add(stop); card.Controls.Add(screenshot); card.Controls.Add(record); card.Controls.Add(stopRecord); card.Controls.Add(hint);
            return card;
        }

        private Control BuildPreviewCard()
        {
            var card = new CardPanel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(1) };
            var head = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Surface, Padding = new Padding(12, 0, 12, 0) };
            var title = MakeLabel("PREVIEW", 8F, FontStyle.Bold, TextMain); title.Dock = DockStyle.Left; title.Width = 100; title.TextAlign = ContentAlignment.MiddleLeft;
            formatInfo.Text = "NO SIGNAL"; formatInfo.Font = new Font("Segoe UI", 7F, FontStyle.Bold); formatInfo.ForeColor = TextMuted; formatInfo.Dock = DockStyle.Right; formatInfo.Width = 190; formatInfo.TextAlign = ContentAlignment.MiddleRight;
            Localization.Bind(title, "Preview"); head.Controls.Add(formatInfo); head.Controls.Add(title);

            var previewFrame = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(8, 0, 8, 8) };
            preview.Dock = DockStyle.Fill; preview.BackColor = Color.FromArgb(4, 6, 9);
            previewPlaceholder.Text = "NO VIDEO SOURCE\r\n\r\nSelect a webcam and start the preview";
            previewPlaceholder.ForeColor = TextMuted; previewPlaceholder.BackColor = Color.Transparent; previewPlaceholder.TextAlign = ContentAlignment.MiddleCenter;
            previewPlaceholder.Font = new Font("Segoe UI", 9F); previewPlaceholder.AutoSize = false; previewPlaceholder.Dock = DockStyle.Fill;
            ConfigureToolbarButton(fullscreen, ButtonIcon.Fullscreen, Color.FromArgb(25, 33, 43), Accent, "Full screen"); fullscreen.Size = new Size(42, 42); fullscreen.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            preview.Controls.Add(previewPlaceholder); preview.Controls.Add(fullscreen); fullscreen.BringToFront();
            previewFrame.Controls.Add(preview); card.Controls.Add(previewFrame); card.Controls.Add(head);
            return card;
        }

        private void PositionFullscreenButton()
        {
            fullscreen.Location = new Point(Math.Max(8, preview.ClientSize.Width - fullscreen.Width - 12), Math.Max(8, preview.ClientSize.Height - fullscreen.Height - 12));
            fullscreen.BringToFront();
        }
        private Control BuildLogHeader()
        {
            var head = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Surface, Padding = new Padding(10, 0, 8, 0) };
            var title = MakeLabel("DIAGNOSTIC LOG", 7.5F, FontStyle.Bold, TextMain); title.Dock = DockStyle.Left; title.Width = 180; title.TextAlign = ContentAlignment.MiddleLeft;
            toggleLog.Text = "NASCONDI"; toggleLog.Dock = DockStyle.Right; toggleLog.Width = 78; toggleLog.BaseColor = Surface; toggleLog.HoverColor = SurfaceLight; toggleLog.ForeColor = TextMuted;
            Localization.Bind(title, "DiagnosticLog"); Localization.Bind(toggleLog, "Hide");
            head.Controls.Add(toggleLog); head.Controls.Add(title); return head;
        }

        private Control BuildLogBody()
        {
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(9, 0, 9, 8) };
            logBox.Dock = DockStyle.Fill; logBox.Multiline = true; logBox.ReadOnly = true; logBox.ScrollBars = ScrollBars.Vertical; logBox.WordWrap = false;
            logBox.BorderStyle = BorderStyle.None; logBox.Font = new Font("Consolas", 7.25F); logBox.BackColor = Color.FromArgb(12, 16, 22); logBox.ForeColor = Color.FromArgb(174, 187, 201);
            body.Controls.Add(logBox); return body;
        }

        private Control BuildStatusBar()
        {
            var bar = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.FromArgb(17, 22, 29), Padding = new Padding(12, 0, 12, 0) };
            var dot = new StatusDot { Dock = DockStyle.Left, Width = 18, DotColor = Success };
            state.Text = "Pronto"; state.ForeColor = TextMuted; state.Dock = DockStyle.Fill; state.TextAlign = ContentAlignment.MiddleLeft; state.AutoEllipsis = true;
            var version = MakeLabel("VISUPRA7  1.0", 8F, FontStyle.Bold, TextMuted); version.Dock = DockStyle.Right; version.Width = 110; version.TextAlign = ContentAlignment.MiddleRight;
            bar.Paint += delegate(object sender, PaintEventArgs e) { using (var pen = new Pen(Border)) e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0); };
            Localization.Bind(state, "Ready");
            bar.Controls.Add(state); bar.Controls.Add(dot); bar.Controls.Add(version); return bar;
        }

        private static Label MakeLabel(string text, float size, FontStyle style, Color color)
        {
            return new Label { Text = text, Font = new Font("Segoe UI", size, style), ForeColor = color, BackColor = Color.Transparent };
        }

        private static void ConfigureCombo(ComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList; combo.FlatStyle = FlatStyle.Flat; combo.BackColor = SurfaceLight; combo.ForeColor = TextMain;
            combo.Font = new Font("Segoe UI", 9F); combo.IntegralHeight = false; combo.DropDownHeight = 220; combo.DrawMode = DrawMode.OwnerDrawFixed; combo.ItemHeight = 22;
            combo.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                if (e.Index < 0) return; bool selected = (e.State & DrawItemState.Selected) != 0;
                using (var brush = new SolidBrush(selected ? Accent : SurfaceLight)) e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, combo.Items[e.Index].ToString(), combo.Font, e.Bounds, selected ? Color.White : TextMain, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };
        }

        private void WireEvents()
        {
            detect.Click += delegate { DetectDevices(); }; devices.SelectedIndexChanged += delegate { LoadFormats(); }; start.Click += async delegate { await StartCamera(); };
            stop.Click += async delegate { if (recorder.IsRecording) await StopRecording(); else await StopCamera(); }; screenshot.Click += async delegate { await TakeScreenshot(); }; record.Click += delegate { StartRecording(); };
            stopRecord.Click += async delegate { await StopRecording(); }; toggleLog.Click += delegate { ToggleLog(); }; fullscreen.Click += delegate { OpenFullscreen(); }; language.SelectedIndexChanged += delegate { ChangeLanguage(); };
            preview.Resize += delegate { capture.ResizePreview(preview.ClientSize.Width, preview.ClientSize.Height); PositionFullscreenButton(); }; preview.DoubleClick += delegate { OpenFullscreen(); }; FormClosing += OnClosing;
            UpdateToolTips();
        }

        private void UpdateToolTips()
        {
            tips.SetToolTip(start, Localization.T("StartTip")); tips.SetToolTip(record, Localization.T("RecordTip"));
            tips.SetToolTip(stop, Localization.T("StopTip")); tips.SetToolTip(screenshot, Localization.T("ScreenshotTip"));
            tips.SetToolTip(fullscreen, Localization.T("FullscreenTip")); tips.SetToolTip(detect, Localization.T("DetectTip"));
        }
        private void OpenFullscreen()
        {
            if (!capture.IsRunning || fullscreenWindow != null) return;
            FullscreenPreviewForm window = new FullscreenPreviewForm(StartRecording, StopRecording, TakeScreenshot);
            fullscreenWindow = window;
            window.PreviewSizeChanged += delegate(int w, int h) { capture.ResizePreview(w, h); };
            window.FormClosing += delegate
            {
                try { if (capture.IsRunning) capture.AttachPreview(preview.Handle, preview.ClientSize.Width, preview.ClientSize.Height); }
                catch (Exception ex) { log.Error("Failed to restore preview after full screen", ex); }
                fullscreenWindow = null; SetState(Localization.T(capture.IsRunning ? "PreviewRestored" : "CameraStopped"));
            };
            try
            {
                window.Show(this); capture.AttachPreview(window.PreviewHandle, window.PreviewSize.Width, window.PreviewSize.Height); window.Activate();
                SetState(Localization.T("FullscreenState"));
            }
            catch (Exception ex) { log.Error("Failed to open full screen", ex); window.Close(); MessageBox.Show(DialogOwner, ex.Message, Localization.T("FullscreenError"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private IWin32Window DialogOwner { get { return fullscreenWindow == null ? (IWin32Window)this : fullscreenWindow; } }

        private void ChangeLanguage()
        {
            Localization.SetLanguage(language.SelectedIndex == 1 ? "it" : "en"); ApplyTranslations();
            if (formats.Items.Count > 0 && formats.Items[0] is string) formats.Items[0] = Localization.T("DefaultFormat");
            if (capture.IsRunning) { SetConnectionState(Localization.T("Live"), Success); SetState(Localization.T("PreviewActive", devices.SelectedItem)); }
            else if (devices.Items.Count == 0) { SetConnectionState(Localization.T("NoCamera"), Danger); SetState(Localization.T("NoCameraDetected")); }
            else { SetConnectionState(Localization.T("Ready"), Success); SetState(Localization.T("CamerasDetected", devices.Items.Count)); }
        }

        private void ApplyTranslations()
        {
            Localization.Apply(this); previewPlaceholder.Text = Localization.T("NoVideoSource") + "\r\n\r\n" + Localization.T("NoVideoHint");
            UpdateToolTips();
            if (!capture.IsRunning) formatInfo.Text = Localization.T("NoSignal"); toggleLog.Text = Localization.T(logExpanded ? "Hide" : "Show");
        }

        private void ToggleLog() { logExpanded = !logExpanded; logCard.Height = logExpanded ? 116 : 30; toggleLog.Text = Localization.T(logExpanded ? "Hide" : "Show"); }

        private async void DetectDevices()
        {
            if (capture.IsRunning) return; detect.Enabled = false; SetState(Localization.T("Detecting")); SetConnectionState(Localization.T("Searching"), TextMuted);
            foreach (WebcamDevice d in deviceList) d.Dispose(); deviceList.Clear(); devices.Items.Clear(); formats.Items.Clear();
            deviceList = await Task.Run(delegate { return WebcamCapture.Enumerate(log); });
            foreach (WebcamDevice d in deviceList) devices.Items.Add(d); if (devices.Items.Count > 0) devices.SelectedIndex = 0;
            SetState(devices.Items.Count == 0 ? Localization.T("NoCameraDetected") : Localization.T("CamerasDetected", devices.Items.Count));
            SetConnectionState(devices.Items.Count == 0 ? Localization.T("NoCamera") : Localization.T("Ready"), devices.Items.Count == 0 ? Danger : Success); detect.Enabled = true; UpdateButtons();
        }

        private void LoadFormats()
        {
            formats.Items.Clear(); WebcamDevice selected = devices.SelectedItem as WebcamDevice; if (selected == null || capture.IsRunning) return;
            formats.Items.Add(Localization.T("DefaultFormat")); formats.SelectedIndex = 0;
            log.Info("Using the default webcam format; DirectShow capability query is disabled for Windows 7 compatibility.");
            UpdateButtons();
        }

        private async Task StartCamera()
        {
            WebcamDevice device = devices.SelectedItem as WebcamDevice; if (device == null) return; VideoFormat format = formats.SelectedItem as VideoFormat;
            SetBusy(true); SetState(Localization.T("StartingCamera")); SetConnectionState(Localization.T("Connecting"), Accent);
            try
            {
                await Task.Yield();
                capture.Start(device, format, preview.Handle, preview.ClientSize.Width, preview.ClientSize.Height);
                previewPlaceholder.Visible = false; fullscreen.BringToFront(); formatInfo.Text = capture.Width + " × " + capture.Height + "  /  " + capture.Fps + " FPS";
                SetConnectionState(Localization.T("Live"), Success); SetState(Localization.T("PreviewActive", device.Name));
            }
            catch (Exception ex)
            {
                log.Error("Webcam start failed (device busy, disconnected, or unsupported format)", ex); MessageBox.Show(DialogOwner, Localization.T("CameraStartBody", ex.Message), "Visupra7", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetConnectionState(Localization.T("Error"), Danger); SetState(Localization.T("CameraStartError"));
            }
            finally { SetBusy(false); UpdateButtons(); }
        }

        private async Task StopCamera()
        {
            if (fullscreenWindow != null) fullscreenWindow.Close(); SetBusy(true); if (recorder.IsRecording) await StopRecording(); SetState(Localization.T("StoppingCamera")); capture.Stop();
            previewPlaceholder.Visible = true; previewPlaceholder.BringToFront(); formatInfo.Text = Localization.T("NoSignal"); SetConnectionState(Localization.T("Offline"), TextMuted); SetState(Localization.T("CameraStopped")); SetBusy(false); UpdateButtons();
        }

        private async Task TakeScreenshot()
        {
            FrameData frame = capture.GetLatestFrame(); if (frame == null) { MessageBox.Show(DialogOwner, Localization.T("WaitFirstFrame"), "Visupra7", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            screenshot.Enabled = false;
            try { string path = await Task.Run(delegate { return SaveJpeg(frame); }); log.Info("Screenshot saved: " + path); SetState(Localization.T("ScreenshotSaved", Path.GetFileName(path))); }
            catch (Exception ex) { log.Error("Screenshot failed", ex); MessageBox.Show(DialogOwner, ex.Message, Localization.T("ScreenshotError"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { screenshot.Enabled = true; }
        }

        private string SaveJpeg(FrameData frame)
        {
            Directory.CreateDirectory(settings.ScreenshotFolder); string path = FfmpegRecorder.UniquePath(settings.ScreenshotFolder, ".jpg");
            using (var bitmap = new Bitmap(frame.Width, frame.Height, PixelFormat.Format24bppRgb))
            {
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                try { int bytes = frame.Width * 3; for (int y = 0; y < frame.Height; y++) { int srcY = frame.BottomUp ? frame.Height - 1 - y : y; Marshal.Copy(frame.Buffer, srcY * frame.Stride, IntPtr.Add(data.Scan0, y * data.Stride), bytes); } }
                finally { bitmap.UnlockBits(data); }
                ImageCodecInfo jpeg = Array.Find(ImageCodecInfo.GetImageEncoders(), delegate(ImageCodecInfo c) { return c.FormatID == ImageFormat.Jpeg.Guid; });
                using (var parameters = new EncoderParameters(1)) { parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, settings.JpegQuality); bitmap.Save(path, jpeg, parameters); }
            }
            return path;
        }

        private void StartRecording()
        {
            try { FrameData frame = capture.GetLatestFrame(); if (frame == null) throw new InvalidOperationException(Localization.T("WaitFirstFrame")); recorder.Start(frame.Width, frame.Height, capture.Fps, frame.BottomUp); SetState(Localization.T("RecordingStarted", Path.GetFileName(recorder.OutputPath))); }
            catch (Exception ex) { log.Error("Recording start failed", ex); MessageBox.Show(DialogOwner, ex.Message, Localization.T("RecordingNotStarted"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            UpdateButtons();
        }

        private async Task StopRecording()
        {
            if (!recorder.IsRecording) return; stopRecord.Enabled = false; SetState(Localization.T("FinalizingMp4")); RecordingResult result = await recorder.StopAsync();
            if (result.Success) SetState(Localization.T("RecordingSaved", Path.GetFileName(result.Path))); else { SetState(Localization.T("RecordingFailed")); MessageBox.Show(DialogOwner, result.Error, Localization.T("RecordingError"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            UpdateButtons();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            rec.Visible = recorder.IsRecording; elapsed.Text = recorder.IsRecording ? recorder.Elapsed.ToString(@"hh\:mm\:ss") : "00:00:00"; if (fullscreenWindow != null) fullscreenWindow.SetRecording(recorder.IsRecording, recorder.Elapsed);
            if (capture.IsRunning && capture.PollDeviceLost()) { log.Warn("Device disconnected or graph completed unexpectedly"); SetState(Localization.T("DeviceLost")); SetConnectionState(Localization.T("SignalLost"), Danger); Task ignoredStop = StopCamera(); }
        }

        private void SetConnectionState(string value, Color color) { connectionState.Text = value; connectionState.ForeColor = color; }
        private void RecorderFailed(string message) { if (!IsDisposed) BeginInvoke((Action)delegate { SetState(Localization.T("EncoderError")); UpdateButtons(); }); }
        private void AppendLog(string line) { if (IsDisposed || closing) return; if (InvokeRequired) { try { BeginInvoke((Action<string>)AppendLog, line); } catch { } return; } logBox.AppendText(line + Environment.NewLine); if (logBox.TextLength > 250000) logBox.Text = logBox.Text.Substring(100000); }
        private void SetState(string value) { state.Text = value; }
        private void SetBusy(bool busy) { detect.Enabled = !busy; start.Enabled = !busy; stop.Enabled = !busy; devices.Enabled = !busy; formats.Enabled = !busy; }
        private void UpdateButtons()
        {
            bool running = capture.IsRunning, recording = recorder.IsRecording; start.Enabled = !running && devices.SelectedItem != null; stop.Enabled = running; screenshot.Enabled = running; record.Enabled = running && !recording; stopRecord.Enabled = recording; fullscreen.Enabled = running; detect.Enabled = !running; devices.Enabled = !running; formats.Enabled = !running;
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (closing) return; closing = true; timer.Stop(); if (fullscreenWindow != null) fullscreenWindow.Close(); Enabled = false; log.Info("Closing requested; releasing recording and webcam");
            try { if (recorder.IsRecording) recorder.StopAsync().Wait(18000); } catch (Exception ex) { log.Error("Recording shutdown error", ex); }
            try { capture.Stop(); } catch (Exception ex) { log.Error("Webcam shutdown error", ex); }
            foreach (WebcamDevice d in deviceList) d.Dispose(); recorder.Dispose(); capture.Dispose(); log.LineWritten -= AppendLog;
        }

        private sealed class CardPanel : Panel
        {
            public CardPanel() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
            protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); using (var pen = new Pen(Border)) e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1); }
        }

        private enum ButtonIcon { None, Play, Record, Stop, Capture, Fullscreen }

        private sealed class ModernButton : Button
        {
            private bool hovering, pressed;
            public Color BaseColor { get; set; } public Color HoverColor { get; set; } public ButtonIcon Icon { get; set; }
            public ModernButton()
            {
                BaseColor = SurfaceLight; HoverColor = Accent; ForeColor = Color.White; Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
                Cursor = Cursors.Hand; TabStop = true; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            }
            protected override void OnMouseEnter(EventArgs e) { hovering = true; Invalidate(); base.OnMouseEnter(e); }
            protected override void OnMouseLeave(EventArgs e) { hovering = pressed = false; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
            protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
            protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; Color fill = !Enabled ? Color.FromArgb(31, 38, 47) : pressed ? ControlPaint.Dark(HoverColor, .08F) : hovering ? HoverColor : BaseColor;
                using (GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 7)) using (var brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
                Color iconColor = Enabled ? ForeColor : Color.FromArgb(90, 101, 114);
                if (Icon == ButtonIcon.None) TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, iconColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                else DrawIcon(e.Graphics, iconColor);
                if (Focused && ShowFocusCues) { Rectangle focus = ClientRectangle; focus.Inflate(-4, -4); ControlPaint.DrawFocusRectangle(e.Graphics, focus, iconColor, fill); }
            }
            private void DrawIcon(Graphics graphics, Color color)
            {
                int cx = Width / 2, cy = Height / 2;
                using (var pen = new Pen(color, 2F)) using (var brush = new SolidBrush(color))
                {
                    pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                    if (Icon == ButtonIcon.Play) graphics.FillPolygon(brush, new[] { new Point(cx - 6, cy - 9), new Point(cx + 9, cy), new Point(cx - 6, cy + 9) });
                    else if (Icon == ButtonIcon.Record) graphics.FillEllipse(brush, cx - 8, cy - 8, 16, 16);
                    else if (Icon == ButtonIcon.Stop) graphics.FillRectangle(brush, cx - 7, cy - 7, 14, 14);
                    else if (Icon == ButtonIcon.Capture)
                    {
                        graphics.DrawRectangle(pen, cx - 11, cy - 7, 22, 15); graphics.DrawRectangle(pen, cx - 5, cy - 10, 10, 3); graphics.DrawEllipse(pen, cx - 5, cy - 5, 10, 10);
                    }
                    else if (Icon == ButtonIcon.Fullscreen)
                    {
                        graphics.DrawLines(pen, new[] { new Point(cx - 3, cy - 9), new Point(cx - 9, cy - 9), new Point(cx - 9, cy - 3) });
                        graphics.DrawLines(pen, new[] { new Point(cx + 3, cy - 9), new Point(cx + 9, cy - 9), new Point(cx + 9, cy - 3) });
                        graphics.DrawLines(pen, new[] { new Point(cx - 9, cy + 3), new Point(cx - 9, cy + 9), new Point(cx - 3, cy + 9) });
                        graphics.DrawLines(pen, new[] { new Point(cx + 9, cy + 3), new Point(cx + 9, cy + 9), new Point(cx + 3, cy + 9) });
                    }
                }
            }
        }
        private sealed class LogoMark : Control
        {
            public LogoMark() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent; }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; Rectangle r = new Rectangle(1, 1, Width - 3, Height - 3);
                using (GraphicsPath path = RoundedRect(r, 10)) using (var brush = new LinearGradientBrush(r, Accent, Color.FromArgb(106, 72, 255), 45F)) e.Graphics.FillPath(brush, path);
                using (var pen = new Pen(Color.White, 2F)) { e.Graphics.DrawRectangle(pen, 10, 13, 18, 14); e.Graphics.DrawLine(pen, 28, 17, 34, 13); e.Graphics.DrawLine(pen, 34, 13, 34, 28); e.Graphics.DrawLine(pen, 34, 28, 28, 24); }
            }
        }

        private sealed class StatusDot : Control
        {
            public Color DotColor { get; set; }
            public StatusDot() { SetStyle(ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent; }
            protected override void OnPaint(PaintEventArgs e) { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (var brush = new SolidBrush(DotColor)) e.Graphics.FillEllipse(brush, 3, Height / 2 - 3, 6, 6); }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2; var path = new GraphicsPath(); if (bounds.Width <= 0 || bounds.Height <= 0) return path;
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90); path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90); path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
        }
    }
}
