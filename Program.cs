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
            using (var log = new Logger(settings.LogFolder))
            {
                Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e) { log.Error("Eccezione UI non gestita", e.Exception); };
                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { log.Error("Eccezione non gestita", e.ExceptionObject as Exception); };
                AssemblyName name = Assembly.GetExecutingAssembly().GetName();
                log.Info("Avvio " + name.Name + " " + name.Version);
                log.Info("Sistema operativo: " + Environment.OSVersion + "; processo: " + (IntPtr.Size * 8) + " bit; CLR: " + Environment.Version);
                try { Application.Run(new MainForm(settings, log)); }
                catch (Exception ex) { log.Error("Errore fatale", ex); MessageBox.Show(ex.Message, "Visupra7 - errore", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                finally { log.Info("Chiusura applicazione"); }
            }
        }
    }
}
