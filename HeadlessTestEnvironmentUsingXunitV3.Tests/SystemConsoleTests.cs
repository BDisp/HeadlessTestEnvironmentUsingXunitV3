using System.Diagnostics;

namespace HeadlessTestEnvironmentUsingXunitV3.Tests
{
    public class SystemConsoleTests
    {
        [Fact]
        public void SystemConsole_WindowWidth_Behavior_OnHeadlessTestEnvironment()
        {
            // Arrange & Act: try to access Console.WindowWidth and capture any exception
            Exception? exception = Record.Exception(() => { _ = Console.WindowWidth; });

            // Detect if a debugger is attached (e.g., running under F5 in Visual Studio or VS Code)
            bool isAttachedToDebugger = Debugger.IsAttached;

            // Detect if running under ReSharper test runner (which may not throw on Console.WindowWidth)
            string cmd = Environment.CommandLine;
            bool runningUnderReSharper = cmd.Contains("ReSharperTestRunner.dll");
            // Detect if running with the --diagnostic flag (which may indicate a diagnostic run)
            bool runningUnderDiagnostic = cmd.Contains("--diagnostic");
            bool runningUnderDotnetTestPipe = cmd.Contains("--dotnet-test-pipe");

            // If accessing WindowWidth throws, treat the environment as headless/test-host and
            // assert the console I/O is redirected. Otherwise, assume a real console is present.
            if (exception != null)
            {
                Assert.NotNull(exception);
                Assert.True(Console.IsOutputRedirected);
                Assert.True(Console.IsErrorRedirected);
                if (isAttachedToDebugger || !runningUnderReSharper)
                {
                    if (runningUnderReSharper && !runningUnderDiagnostic && !runningUnderDotnetTestPipe &&
                        TestHostHelper.DetectedTestHost != "Visual Studio")
                    {
                        Assert.True(Console.IsInputRedirected);
                    }
                    else
                    {
                        Assert.False(Console.IsInputRedirected);
                    }
                }
                else
                {
                    if (runningUnderReSharper)
                    {
                        Assert.True(Console.IsInputRedirected);
                    }
                    else
                    {
                        Assert.False(Console.IsInputRedirected);
                    }
                }
            }
            else
            {
                Assert.Null(exception);
            }
        }
    }
}