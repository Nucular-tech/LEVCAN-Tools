using System;
using System.IO;
using System.Reflection;

namespace LEVCAN_Configurator
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;

            // Self-test idle detector and frame rate limiter invariants at startup
            Helpers.IdleDetector.RunSelfTest();
            Helpers.FrameRateLimiter.RunSelfTest();

            if (args != null && args.Length > 0 && (args[0] == "--selftest" || args[0] == "-test"))
            {
                // Verify LEVCANlib P/Invoke entry points
                using (var testNode = new LEVCAN.LC_Node(120))
                {
                    if (testNode.DescriptorPtr == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("LC_Node_Create returned null pointer.");
                    }
                }
                Console.WriteLine("IdleDetector, FrameRateLimiter, and LC_Node P/Invoke self-tests passed successfully!");
                return;
            }

            try
            {
                if (args != null && args.Length > 0 && args[0] == "--test-crash")
                {
                    throw new InvalidOperationException("Test crash logging exception");
                }

                var main = new MainMenu();
                main.RunMain();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleException(ex);
            }
            else
            {
                HandleException(new Exception($"Unhandled non-Exception object: {e.ExceptionObject}"));
            }
        }

        private static void GlobalThreadExceptionHandler(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void HandleException(Exception ex)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Crash exception occurred:");
                sb.AppendLine(ex.ToString());
                sb.AppendLine("Hresult: 0x" + ex.HResult.ToString("X8"));

                var st = new System.Diagnostics.StackTrace(ex, true);
                var frame = st.GetFrame(0);
                if (frame != null)
                {
                    sb.AppendLine("Frame: " + frame.ToString());
                    sb.AppendLine("File: " + frame.GetFileName() + ":" + frame.GetFileLineNumber());
                }

                if (ex is ReflectionTypeLoadException typeLoadException)
                {
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                    if (loaderExceptions != null)
                    {
                        foreach (var loadex in loaderExceptions)
                        {
                            if (loadex != null)
                                sb.AppendLine("LoaderException: " + loadex.ToString());
                        }
                    }
                }

                string crashMessage = sb.ToString();

                // Console / debug output
                Console.Error.WriteLine(crashMessage);
                System.Diagnostics.Debug.WriteLine(crashMessage);
                System.Diagnostics.Trace.WriteLine(crashMessage);

                // Write to crash_log.txt in AppDomain.CurrentDomain.BaseDirectory
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                using (var stream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(crashMessage);
                    writer.Flush();
                    stream.Flush(true);
                }
            }
            catch (Exception loggingEx)
            {
                try
                {
                    Console.Error.WriteLine("Failed to write crash log: " + loggingEx);
                }
                catch
                {
                }
            }
        }
    }
}
