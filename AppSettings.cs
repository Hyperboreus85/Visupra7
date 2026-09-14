using System;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace Visupra7
{
    internal sealed class AppSettings
    {
        public readonly string BaseFolder = AppDomain.CurrentDomain.BaseDirectory;
        public string ScreenshotFolder { get; private set; }
        public string RecordingFolder { get; private set; }
        public string LogFolder { get; private set; }
        public string FfmpegPath { get; private set; }
        public string Language { get; private set; }
        public long JpegQuality { get; private set; }
        public int H264Crf { get; private set; }
        public string H264Preset { get; private set; }
        public int MaxRecordingFps { get; private set; }
        public int PreferredWidth { get; private set; }
        public int PreferredHeight { get; private set; }
        public int SegmentMinutes { get; private set; }

        public AppSettings()
        {
            ScreenshotFolder = Resolve(Get("ScreenshotFolder", "Screenshots"));
            RecordingFolder = Resolve(Get("RecordingFolder", "Recordings"));
            LogFolder = Resolve(Get("LogFolder", "Logs"));
            FfmpegPath = Resolve(Get("FfmpegRelativePath", @"Tools\ffmpeg.exe"));
            Language = Get("Language", "en");
            JpegQuality = Clamp(GetInt("JpegQuality", 85), 1, 100);
            H264Crf = Clamp(GetInt("H264Crf", 23), 0, 51);
            H264Preset = Get("H264Preset", "veryfast");
            MaxRecordingFps = Clamp(GetInt("MaxRecordingFps", 30), 1, 120);
            PreferredWidth = Clamp(GetInt("PreferredWidth", 1280), 160, 7680);
            PreferredHeight = Clamp(GetInt("PreferredHeight", 720), 120, 4320);
            SegmentMinutes = Clamp(GetInt("SegmentMinutes", 0), 0, 1440);
        }

        private string Resolve(string value) { return Path.GetFullPath(Path.IsPathRooted(value) ? value : Path.Combine(BaseFolder, value)); }
        private static string Get(string key, string fallback) { return ConfigurationManager.AppSettings[key] ?? fallback; }
        private static int GetInt(string key, int fallback) { int v; return int.TryParse(Get(key, ""), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : fallback; }
        private static int Clamp(int value, int min, int max) { return Math.Max(min, Math.Min(max, value)); }
    }
}
