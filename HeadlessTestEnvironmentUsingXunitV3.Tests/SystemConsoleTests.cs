using System.Diagnostics;
using System.Runtime.InteropServices;

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
            // Detect common CI environments (GitHub Actions / generic CI)
            bool isCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
                        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

            // Platform-aware expectations: Linux and macOS share behavior; Windows may differ.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: decide expected exception based on command-line indicators, debugger, ReSharper, and CI
                bool expectException;
                if (runningUnderReSharper)
                {
                    expectException = true;
                }
                else if (isAttachedToDebugger)
                {
                    expectException = false;
                }
                else if (runningUnderDiagnostic || runningUnderDotnetTestPipe || isCi)
                {
                    expectException = true;
                }
                else
                {
                    expectException = false;
                }

                if (expectException || runningUnderReSharper)
                {
                    Assert.NotNull(exception);
                    Assert.True(Console.IsOutputRedirected);
                    Assert.True(Console.IsErrorRedirected);
                    if ((!isAttachedToDebugger && runningUnderReSharper)
                        || (isAttachedToDebugger && runningUnderReSharper &&
                            TestHostHelper.DetectedTestHost == "VS Code (VSCODE_PID)")
                        || isCi)
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
                    Assert.Null(exception);
                    Assert.False(Console.IsOutputRedirected);
                    Assert.False(Console.IsErrorRedirected);
                    Assert.False(Console.IsInputRedirected);
                }
            }
            else
            {
                // Non-Windows platforms (Linux, macOS, etc.) share the same logic
                bool expectException = TestHostHelper.DetectedTestHost != "VS Code (TERM_PROGRAM)" &&
                                       (runningUnderDiagnostic || runningUnderDotnetTestPipe || isCi ||
                                        runningUnderReSharper);
                Console.WriteLine($"expectException: {expectException}");
                Console.WriteLine($"DetectedTestHost: {TestHostHelper.DetectedTestHost}");
                if (expectException && !runningUnderReSharper)
                {
                    Assert.NotNull(exception);
                    Assert.True(Console.IsOutputRedirected);
                    Assert.True(Console.IsErrorRedirected);
                    Assert.True(Console.IsInputRedirected);
                }
                else
                {
                    Assert.Null(exception);
                    if ((isAttachedToDebugger && !runningUnderReSharper)
                        || TestHostHelper.DetectedTestHost == "VS Code (TERM_PROGRAM)")
                    {
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                    else
                    {
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        Assert.True(Console.IsInputRedirected);
                    }
                }
            }
        }
    }
}