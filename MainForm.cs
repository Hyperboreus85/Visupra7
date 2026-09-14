using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Visupra7
{
    internal sealed class MainForm : Form
    {
        private readonly AppSettings settings; private readonly Logger log; private readonly WebcamCapture capture; private readonly FfmpegRecorder recorder;
        private readonly ComboBox devices = new ComboBox(), formats = new ComboBox(); private readonly Button detect = new Button(), start = new Button(), stop = new Button(), screenshot = new Button(), record = new Button(), stopRecord = new Button();
        private readonly Panel preview = new Panel(); private readonly TextBox logBox = new TextBox(); private readonly Label state = new Label(), rec = new Label(), elapsed = new Label();
        private readonly Timer timer = new Timer(); private List<WebcamDevice> deviceList = new List<WebcamDevice>(); private bool closing;

        public MainForm(AppSettings appSettings, Logger logger)
        {
            settings = appSettings; log = logger; capture = new WebcamCapture(log); recorder = new FfmpegRecorder(settings, log); capture.FrameArrived += recorder.Enqueue; recorder.Failed += RecorderFailed;
            Text = "Visupra7"; MinimumSize = new Size(820, 600); Size = new Size(1100, 760); StartPosition = FormStartPosition.CenterScreen;
            BuildUi(); WireEvents(); log.LineWritten += AppendLog; timer.Interval = 250; timer.Tick += TimerTick; timer.Start(); UpdateButtons();
            Shown += delegate { DetectDevices(); };
        }

        private void BuildUi()
        {
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 74, Padding = new Padding(7), AutoSize = false, WrapContents = true };
            devices.Width = 230; devices.DropDownStyle = ComboBoxStyle.DropDownList; formats.Width = 190; formats.DropDownStyle = ComboBoxStyle.DropDownList;
            detect.Text = "Rileva webcam"; start.Text = "Avvia"; stop.Text = "Ferma"; screenshot.Text = "Screenshot"; record.Text = "Registra"; stopRecord.Text = "Stop registrazione";
            foreach (Button b in new[] { detect, start, stop, screenshot, record, stopRecord }) { b.AutoSize = true; b.Height = 29; }
            top.Controls.Add(new Label { Text = "Webcam:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }); top.Controls.Add(devices); top.Controls.Add(detect);
            top.Controls.Add(new Label { Text = "Formato:", AutoSize = true, Padding = new Padding(5, 7, 0, 0) }); top.Controls.Add(formats); top.Controls.Add(start); top.Controls.Add(stop); top.Controls.Add(screenshot); top.Controls.Add(record); top.Controls.Add(stopRecord);
            preview.Dock = DockStyle.Fill; preview.BackColor = Color.Black; preview.BorderStyle = BorderStyle.FixedSingle;
            logBox.Dock = DockStyle.Fill; logBox.Multiline = true; logBox.ReadOnly = true; logBox.ScrollBars = ScrollBars.Both; logBox.WordWrap = false; logBox.Font = new Font("Consolas", 9F);
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 480, Panel1MinSize = 200, Panel2MinSize = 100 }; split.Panel1.Controls.Add(preview); split.Panel2.Controls.Add(logBox);
            var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 30, ColumnCount = 3, Padding = new Padding(7, 4, 7, 2) }; bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            state.Text = "Pronto"; state.AutoEllipsis = true; rec.Text = "● REC"; rec.ForeColor = Color.Red; rec.Font = new Font(Font, FontStyle.Bold); rec.Visible = false; elapsed.Text = "00:00:00"; elapsed.TextAlign = ContentAlignment.MiddleRight;
            bottom.Controls.Add(state, 0, 0); bottom.Controls.Add(rec, 1, 0); bottom.Controls.Add(elapsed, 2, 0);
            Controls.Add(split); Controls.Add(bottom); Controls.Add(top);
        }

        private void WireEvents()
        {
            detect.Click += delegate { DetectDevices(); }; devices.SelectedIndexChanged += delegate { LoadFormats(); }; start.Click += async delegate { await StartCamera(); };
            stop.Click += async delegate { await StopCamera(); }; screenshot.Click += async delegate { await TakeScreenshot(); }; record.Click += delegate { StartRecording(); };
            stopRecord.Click += async delegate { await StopRecording(); }; preview.Resize += delegate { capture.ResizePreview(preview.ClientSize.Width, preview.ClientSize.Height); };
            FormClosing += OnClosing;
        }

        private async void DetectDevices()
        {
            if (capture.IsRunning) return; detect.Enabled = false; SetState("Rilevamento webcam...");
            foreach (WebcamDevice d in deviceList) d.Dispose(); deviceList.Clear(); devices.Items.Clear(); formats.Items.Clear();
            deviceList = await Task.Run(delegate { return WebcamCapture.Enumerate(log); });
            foreach (WebcamDevice d in deviceList) devices.Items.Add(d); if (devices.Items.Count > 0) devices.SelectedIndex = 0;
            SetState(devices.Items.Count == 0 ? "Nessuna webcam rilevata" : devices.Items.Count + " webcam rilevate"); detect.Enabled = true; UpdateButtons();
        }

        private async void LoadFormats()
        {
            formats.Items.Clear(); WebcamDevice selected = devices.SelectedItem as WebcamDevice; if (selected == null || capture.IsRunning) return;
            formats.Items.Add("Predefinito webcam"); formats.SelectedIndex = 0; devices.Enabled = false;
            List<VideoFormat> values = await Task.Run(delegate { return WebcamCapture.GetFormats(selected, log); });
            foreach (VideoFormat f in values) formats.Items.Add(f);
            int best = 0, bestScore = int.MaxValue;
            for (int i = 0; i < values.Count; i++) { int score = Math.Abs(values[i].Width - settings.PreferredWidth) + Math.Abs(values[i].Height - settings.PreferredHeight); if (score < bestScore) { bestScore = score; best = i + 1; } }
            if (formats.Items.Count > 1) formats.SelectedIndex = best; devices.Enabled = true; UpdateButtons();
        }

        private async Task StartCamera()
        {
            WebcamDevice device = devices.SelectedItem as WebcamDevice; if (device == null) return; VideoFormat format = formats.SelectedItem as VideoFormat;
            SetBusy(true); SetState("Avvio webcam...");
            try { await Task.Run(delegate { capture.Start(device, format, preview.Handle, preview.ClientSize.Width, preview.ClientSize.Height); }); SetState("Anteprima attiva: " + capture.Width + "x" + capture.Height + " @ " + capture.Fps + " fps"); }
            catch (Exception ex) { log.Error("Avvio webcam fallito (dispositivo occupato, scollegato o formato non supportato)", ex); MessageBox.Show(this, "Impossibile avviare la webcam.\r\n\r\n" + ex.Message, "Visupra7", MessageBoxButtons.OK, MessageBoxIcon.Error); SetState("Errore avvio webcam"); }
            finally { SetBusy(false); UpdateButtons(); }
        }

        private async Task StopCamera()
        {
            SetBusy(true); if (recorder.IsRecording) await StopRecording(); SetState("Arresto webcam..."); await Task.Run(delegate { capture.Stop(); }); SetState("Webcam ferma"); SetBusy(false); UpdateButtons();
        }

        private async Task TakeScreenshot()
        {
            FrameData frame = capture.GetLatestFrame(); if (frame == null) { MessageBox.Show(this, "Non è ancora disponibile un frame.", "Visupra7", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            screenshot.Enabled = false;
            try
            {
                string path = await Task.Run(delegate { return SaveJpeg(frame); }); log.Info("Screenshot salvato: " + path); SetState("Screenshot: " + path);
            }
            catch (Exception ex) { log.Error("Screenshot fallito", ex); MessageBox.Show(this, ex.Message, "Errore screenshot", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
            try { FrameData frame = capture.GetLatestFrame(); if (frame == null) throw new InvalidOperationException("Attendere il primo frame della webcam."); recorder.Start(frame.Width, frame.Height, capture.Fps, frame.BottomUp); SetState("Registrazione: " + recorder.OutputPath); }
            catch (Exception ex) { log.Error("Avvio registrazione fallito", ex); MessageBox.Show(this, ex.Message, "Registrazione non avviata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            UpdateButtons();
        }

        private async Task StopRecording()
        {
            if (!recorder.IsRecording) return; stopRecord.Enabled = false; SetState("Finalizzazione MP4..."); RecordingResult result = await recorder.StopAsync();
            if (result.Success) SetState("Registrazione salvata: " + result.Path); else { SetState("Registrazione fallita"); MessageBox.Show(this, result.Error, "Errore registrazione", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            UpdateButtons();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            rec.Visible = recorder.IsRecording; elapsed.Text = recorder.IsRecording ? recorder.Elapsed.ToString(@"hh\:mm\:ss") : "00:00:00";
            if (capture.IsRunning && capture.PollDeviceLost()) { log.Warn("Perdita dispositivo o completamento inatteso del graph"); SetState("Webcam scollegata o flusso interrotto"); Task ignoredStop = StopCamera(); }
        }

        private void RecorderFailed(string message) { if (!IsDisposed) BeginInvoke((Action)delegate { SetState("Encoder in errore; anteprima ancora attiva"); UpdateButtons(); }); }
        private void AppendLog(string line) { if (IsDisposed || closing) return; if (InvokeRequired) { try { BeginInvoke((Action<string>)AppendLog, line); } catch { } return; } logBox.AppendText(line + Environment.NewLine); if (logBox.TextLength > 250000) logBox.Text = logBox.Text.Substring(100000); }
        private void SetState(string value) { state.Text = value; }
        private void SetBusy(bool busy) { detect.Enabled = !busy; start.Enabled = !busy; stop.Enabled = !busy; devices.Enabled = !busy; formats.Enabled = !busy; }
        private void UpdateButtons()
        {
            bool running = capture.IsRunning, recording = recorder.IsRecording; start.Enabled = !running && devices.SelectedItem != null; stop.Enabled = running; screenshot.Enabled = running; record.Enabled = running && !recording; stopRecord.Enabled = recording; detect.Enabled = !running; devices.Enabled = !running; formats.Enabled = !running;
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (closing) return; closing = true; timer.Stop(); Enabled = false; log.Info("Chiusura richiesta; rilascio registrazione e webcam");
            try { if (recorder.IsRecording) recorder.StopAsync().Wait(18000); } catch (Exception ex) { log.Error("Errore chiusura registrazione", ex); }
            try { capture.Stop(); } catch (Exception ex) { log.Error("Errore chiusura webcam", ex); }
            foreach (WebcamDevice d in deviceList) d.Dispose(); recorder.Dispose(); capture.Dispose(); log.LineWritten -= AppendLog;
        }
    }
}
