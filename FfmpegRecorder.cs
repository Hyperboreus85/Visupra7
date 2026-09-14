using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Visupra7
{
    internal sealed class RecordingResult
    {
        public string Path; public TimeSpan Duration; public long Size; public bool Success; public string Error;
    }

    internal sealed class FfmpegRecorder : IDisposable
    {
        private readonly AppSettings settings; private readonly Logger log; private readonly object sync = new object();
        private BlockingCollection<FrameData> queue; private Process process; private Task writerTask; private volatile bool accepting;
        private Stopwatch duration; private long lastAcceptedTicks; private int targetFps; private string outputPath; private int dropped;
        public event Action<string> Failed;
        public bool IsRecording { get { return accepting; } }
        public TimeSpan Elapsed { get { return duration == null ? TimeSpan.Zero : duration.Elapsed; } }
        public string OutputPath { get { return outputPath; } }

        public FfmpegRecorder(AppSettings appSettings, Logger logger) { settings = appSettings; log = logger; }

        public void Start(int width, int height, int sourceFps, bool bottomUp)
        {
            lock (sync)
            {
                if (accepting) throw new InvalidOperationException("Registrazione già attiva.");
                if (!File.Exists(settings.FfmpegPath)) throw new FileNotFoundException("FFmpeg non trovato. Copiare una build compatibile con Windows 7 in Tools\\ffmpeg.exe.", settings.FfmpegPath);
                Directory.CreateDirectory(settings.RecordingFolder); outputPath = UniquePath(settings.RecordingFolder, ".mp4");
                targetFps = Math.Max(1, Math.Min(settings.MaxRecordingFps, sourceFps > 0 ? sourceFps : settings.MaxRecordingFps)); dropped = 0; lastAcceptedTicks = 0;
                string filters = (bottomUp ? "vflip," : "") + "scale=trunc(iw/2)*2:trunc(ih/2)*2";
                string arguments = "-hide_banner -loglevel warning -f rawvideo -pixel_format bgr24 -video_size " + width + "x" + height + " -framerate " + targetFps +
                    " -i pipe:0 -an -vf \"" + filters + "\" -c:v libx264 -preset " + settings.H264Preset + " -crf " + settings.H264Crf.ToString(CultureInfo.InvariantCulture) +
                    " -pix_fmt yuv420p -movflags +faststart -y \"" + outputPath + "\"";
                var start = new ProcessStartInfo(settings.FfmpegPath, arguments) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardError = true, WorkingDirectory = settings.BaseFolder };
                process = new Process { StartInfo = start, EnableRaisingEvents = true }; process.ErrorDataReceived += OnErrorData;
                log.Info("Avvio registrazione: " + outputPath); log.Info("Comando FFmpeg: \"" + settings.FfmpegPath + "\" " + arguments);
                try { if (!process.Start()) throw new InvalidOperationException("Impossibile avviare FFmpeg."); process.BeginErrorReadLine(); log.Info("PID FFmpeg: " + process.Id); }
                catch { process.Dispose(); process = null; throw; }
                queue = new BlockingCollection<FrameData>(4); accepting = true; duration = Stopwatch.StartNew(); writerTask = Task.Run((Action)WriterLoop);
            }
        }

        public void Enqueue(FrameData frame)
        {
            if (!accepting || queue == null) return;
            long now = Stopwatch.GetTimestamp(); long minimum = Stopwatch.Frequency / Math.Max(1, targetFps);
            long previous = Interlocked.Read(ref lastAcceptedTicks); if (previous != 0 && now - previous < minimum) return;
            if (Interlocked.CompareExchange(ref lastAcceptedTicks, now, previous) != previous) return;
            try { var copy = new FrameData((byte[])frame.Buffer.Clone(), frame.Width, frame.Height, frame.Stride, frame.BottomUp); if (!queue.TryAdd(copy)) Interlocked.Increment(ref dropped); } catch (InvalidOperationException) { }
        }

        private void WriterLoop()
        {
            try
            {
                Stream input = process.StandardInput.BaseStream;
                foreach (FrameData frame in queue.GetConsumingEnumerable())
                {
                    int rowBytes = frame.Width * 3;
                    if (frame.Stride == rowBytes) input.Write(frame.Buffer, 0, Math.Min(frame.Buffer.Length, rowBytes * frame.Height));
                    else for (int y = 0; y < frame.Height; y++) input.Write(frame.Buffer, y * frame.Stride, rowBytes);
                }
                input.Flush(); process.StandardInput.Close();
            }
            catch (Exception ex) { log.Error("Pipeline FFmpeg interrotta", ex); var handler = Failed; if (handler != null) handler(ex.Message); }
        }

        public Task<RecordingResult> StopAsync()
        {
            return Task.Run(delegate
            {
                var result = new RecordingResult { Path = outputPath };
                Process localProcess; Task localWriter; BlockingCollection<FrameData> localQueue;
                lock (sync)
                {
                    if (process == null) { result.Error = "Nessuna registrazione attiva."; return result; }
                    accepting = false; if (duration != null) duration.Stop(); result.Duration = duration == null ? TimeSpan.Zero : duration.Elapsed;
                    localProcess = process; localWriter = writerTask; localQueue = queue; try { localQueue.CompleteAdding(); } catch { }
                }
                try
                {
                    if (localWriter != null && !localWriter.Wait(5000)) { log.Warn("Timeout svuotamento coda FFmpeg; termino il processo"); TryKill(localProcess); }
                    if (!localProcess.HasExited && !localProcess.WaitForExit(10000)) { log.Warn("Timeout finalizzazione MP4; termino FFmpeg"); TryKill(localProcess); localProcess.WaitForExit(2000); }
                    result.Success = File.Exists(result.Path) && new FileInfo(result.Path).Length > 0 && localProcess.ExitCode == 0;
                    if (File.Exists(result.Path)) result.Size = new FileInfo(result.Path).Length;
                    if (!result.Success) result.Error = "FFmpeg non ha prodotto un MP4 valido (exit code " + (localProcess.HasExited ? localProcess.ExitCode.ToString() : "timeout") + ").";
                    log.Info("Stop registrazione; durata " + result.Duration + "; frame scartati " + dropped + "; file " + result.Path + "; dimensione " + result.Size + " byte; esito " + result.Success);
                }
                catch (Exception ex) { result.Error = ex.Message; log.Error("Errore durante stop registrazione", ex); TryKill(localProcess); }
                finally
                {
                    lock (sync) { localProcess.Dispose(); localQueue.Dispose(); process = null; queue = null; writerTask = null; duration = null; }
                }
                return result;
            });
        }

        private void OnErrorData(object sender, DataReceivedEventArgs e) { if (!string.IsNullOrWhiteSpace(e.Data)) log.Warn("FFmpeg: " + e.Data); }
        private static void TryKill(Process value) { try { if (value != null && !value.HasExited) value.Kill(); } catch { } }
        public void Dispose() { if (process != null) try { StopAsync().Wait(18000); } catch { TryKill(process); } }
        internal static string UniquePath(string folder, string extension)
        {
            string stem = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"); string path = Path.Combine(folder, stem + extension); int n = 1;
            while (File.Exists(path)) path = Path.Combine(folder, stem + "_" + (n++).ToString(CultureInfo.InvariantCulture) + extension); return path;
        }
    }
}
