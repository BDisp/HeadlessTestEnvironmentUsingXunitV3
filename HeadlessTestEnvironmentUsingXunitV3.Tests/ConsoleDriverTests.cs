using System.Diagnostics;
using System.Drawing;

namespace HeadlessTestEnvironmentUsingXunitV3.Tests
{
    public class ConsoleDriverTests
    {
        [Fact]
        public void ConsoleDriver_Should_Return_DefaultSize_OnHeadlessTestEnvironment()
        {
            // Arrange
            NetDriver consoleDriver = new();

            // Act
            consoleDriver.Init();

            // Detect if a debugger is attached (e.g., running under F5 in Visual Studio or VS Code)
            bool isAttachedToDebugger = Debugger.IsAttached;

            // Detect test-host vs standalone run and assert accordingly.
            string cmd = Environment.CommandLine;
            bool runningUnderTestHost = cmd.Contains("--server") || cmd.Contains("--dotnet-test-pipe") ||
                                        cmd.Contains("--results-directory");
            // Detect if running with the --diagnostic flag (which may indicate a diagnostic run)
            bool runningDiagnostic = cmd.Contains("--diagnostic");
            // Detect if running under WSL (Windows Subsystem for Linux)
            bool runningUnderWsl = cmd.Contains("/mnt/");

            if (!isAttachedToDebugger & runningUnderTestHost & runningDiagnostic & !runningUnderWsl)
            {
                // Under the test host we expect a headless environment: Init falls back to DefaultSize
                Assert.Equal(consoleDriver.DefaultSize, new Size(consoleDriver.Width, consoleDriver.Height));
                Assert.Equal(80, consoleDriver.Width);
                Assert.Equal(25, consoleDriver.Height);

                // Test host usually redirects console I/O
                Assert.True(consoleDriver.IsOutputRedirected);
                Assert.True(consoleDriver.IsErrorRedirected);
                Assert.False(consoleDriver.IsInputRedirected);
            }
            else
            {
                // Standalone run (F5/Ctrl-F5) has a real console; Init should read the actual window size
                // Be defensive: Console.WindowWidth/Height might throw in some environments, so capture safely.
                int actualW, actualH;
                try
                {
                    actualW = Console.WindowWidth;
                    actualH = Console.WindowHeight;
                }
                catch
                {
                    // If accessing Console.WindowWidth/Height throws, fall back to DefaultSize for comparison
                    actualW = consoleDriver.DefaultSize.Width;
                    actualH = consoleDriver.DefaultSize.Height;
                }

                Assert.Equal(actualW, consoleDriver.Width);
                Assert.Equal(actualH, consoleDriver.Height);

                // When running standalone the console is typically not redirected
                Assert.Equal(Console.IsOutputRedirected, consoleDriver.IsOutputRedirected);
                Assert.Equal(Console.IsErrorRedirected, consoleDriver.IsErrorRedirected);
                Assert.Equal(Console.IsInputRedirected, consoleDriver.IsInputRedirected);
            }
        }
    }
}