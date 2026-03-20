using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WinUIEx;
using Path = System.IO.Path;

namespace MiSTerNESPaletteEditor
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {

        public App()
        {
            this.InitializeComponent();

            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LogException("WinUI UnhandledException", e.Exception);
            e.Handled = true; // optional; only if you want to try continuing
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            LogException("AppDomain UnhandledException", e.ExceptionObject as Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException("TaskScheduler UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        private static void LogException(string source, Exception? ex)
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MiSTerNESPaletteEditor",
                    "crash.log");

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                var sb = new StringBuilder();
                sb.AppendLine("========================================");
                sb.AppendLine(DateTime.Now.ToString("O"));
                sb.AppendLine(source);

                if (ex != null)
                {
                    sb.AppendLine(ex.ToString());
                }
                else
                {
                    sb.AppendLine("Exception object was null.");
                }

                File.AppendAllText(path, sb.ToString());
            }
            catch
            {
                // last resort: never throw from logger
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.CenterOnScreen();

            //var windowMangler = WinUIEx.WindowManager.Get(m_window);
            //windowMangler.PersistenceId = null;// "MainWindowPersistanceId";
            //windowMangler.MinWidth = 640;
            //windowMangler.MinHeight = 480;

            //m_window.SystemBackdrop = new MicaBackdrop();

            m_window.Activate();
        }

        private Window? m_window;
    }
}