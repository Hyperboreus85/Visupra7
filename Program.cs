using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Visupra7
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var settings = new AppSettings();
            Localization.SetLanguage(settings.Language);
            using (var log = new Logger(settings.LogFolder))
            {
                Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e) { log.Error("Unhandled UI exception", e.Exception); };
                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { log.Error("Unhandled exception", e.ExceptionObject as Exception); };
                AssemblyName name = Assembly.GetExecutingAssembly().GetName();
                log.Info("Starting " + name.Name + " " + name.Version);
                log.Info("Operating system: " + Environment.OSVersion + "; process: " + (IntPtr.Size * 8) + " bit; CLR: " + Environment.Version);
                try { Application.Run(new MainForm(settings, log)); }
                catch (Exception ex) { log.Error("Fatal error", ex); MessageBox.Show(ex.Message, "Visupra7 - errore", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                finally { log.Info("Application closing"); }
            }
        }
    }
}
