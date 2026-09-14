using System;
using System.IO;
using System.Text;

namespace Visupra7
{
    internal sealed class Logger : IDisposable
    {
        private readonly object sync = new object();
        private readonly StreamWriter writer;
        public event Action<string> LineWritten;
        public string FilePath { get; private set; }

        public Logger(string folder)
        {
            Directory.CreateDirectory(folder);
            FilePath = Path.Combine(folder, "Visupra7.log");
            writer = new StreamWriter(new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false));
            writer.AutoFlush = true;
        }

        public void Info(string message) { Write("INFO", message, null); }
        public void Warn(string message) { Write("WARN", message, null); }
        public void Error(string message, Exception ex) { Write("ERROR", message, ex); }
        private void Write(string level, string message, Exception ex)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + level + "] " + message + (ex == null ? "" : Environment.NewLine + ex);
            lock (sync) { writer.WriteLine(line); }
            var handler = LineWritten;
            if (handler != null) handler(line);
        }
        public void Dispose() { lock (sync) { writer.Dispose(); } }
    }
}
