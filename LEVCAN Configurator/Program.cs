using System;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace LEVCAN_Configurator
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain currentDomain = default(AppDomain);
            currentDomain = AppDomain.CurrentDomain;
            // Handler for unhandled exceptions.
            currentDomain.UnhandledException += GlobalUnhandledExceptionHandler;

            var main = new MainMenu();
            main.RunMain();
        }
        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException((Exception)e.ExceptionObject);
        }

        private static void GlobalThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void HandleException(Exception ex)
        {
            // Get stack trace for the exception with source file information
            var st = new System.Diagnostics.StackTrace(ex, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();

            using (var writer = new System.IO.StreamWriter(File.Create(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "crash_log.txt"))))
            {
                writer.WriteLine(ex.ToString());
                writer.WriteLine("Hresult:" + ex.HResult.ToString());
                writer.WriteLine(frame.ToString());

                if (ex is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                    foreach (var loadex in loaderExceptions)
                        writer.WriteLine(loadex.ToString());
                }
            }
        }
    }
}
