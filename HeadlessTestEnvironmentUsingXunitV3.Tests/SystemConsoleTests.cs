using System.Diagnostics;

namespace HeadlessTestEnvironmentUsingXunitV3.Tests
{
    public class SystemConsoleTests
    {
        public SystemConsoleTests()
        {
            if (!TestHostHelper.IsInitialized)
            {
                TestHostHelper.Initialize();
            }
        }

        [Fact]
        public void SystemConsole_WindowWidth_Behavior_OnHeadlessTestEnvironment()
        {
            // Arrange & Act: try to access Console.WindowWidth and capture any exception
            Exception? exception = Record.Exception(() => { _ = Console.WindowWidth; });

            // Command-line and environment information for understanding the test environment
            string cmd = Environment.CommandLine;
            // Detect if a debugger is attached (e.g., running under F5 in Visual Studio or VS Code)
            bool isAttachedToDebugger = Debugger.IsAttached;
            // Detect if running under dotnet test pipe (which may not throw on Console.WindowWidth)
            bool runningUnderDotnetTestPipe = cmd.Contains("--dotnet-test-pipe");
            // Detect if running under ReSharper test runner (which may throw on Console.WindowWidth)
            bool runningUnderReSharper = cmd.Contains("ReSharperTestRunner.dll");
            // Detect if running with the --server flag (which may indicate a server run)
            bool runningUnderServer = cmd.Contains("--server");
            // Detect common CI environments (GitHub Actions / generic CI)
            bool isCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
                        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));

            // Platform-aware expectations: Windows, Linux and macOS may differ.
            // Decide expected exception based on command-line indicators, debugger, ReSharper, and CI
            if (OperatingSystem.IsWindows())
            {
                if (isAttachedToDebugger)
                {
                    if (runningUnderDotnetTestPipe)
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                    else if (runningUnderReSharper)
                    {
                        Assert.NotNull(exception);
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        if (TestHostHelper.DetectedTestHost == "VS Code (VSCODE_PID)" && !TestHostHelper.IsWsl)
                        {
                            Assert.True(Console.IsOutputRedirected);
                        }
                        else
                        {
                            Assert.False(Console.IsInputRedirected);
                        }
                    }
                    else if (runningUnderServer)
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
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
                    if (runningUnderDotnetTestPipe)
                    {
                        Assert.NotNull(exception);
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                    else if (runningUnderReSharper)
                    {
                        Assert.NotNull(exception);
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        Assert.True(Console.IsInputRedirected);
                    }
                    else if (runningUnderServer)
                    {
                        if (TestHostHelper.DetectedTestHost == "VS Code (VSCODE_PID)")
                        {
                            Assert.Null(exception);
                            Assert.False(Console.IsOutputRedirected);
                            Assert.False(Console.IsErrorRedirected);
                        }
                        else
                        {
                            Assert.NotNull(exception);
                            Assert.True(Console.IsOutputRedirected);
                            Assert.True(Console.IsErrorRedirected);
                        }
                        Assert.False(Console.IsInputRedirected);
                    }
                    else if (isCi)
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                    else
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                if (isAttachedToDebugger)
                {
                    if (runningUnderDotnetTestPipe)
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                    else if (runningUnderReSharper)
                    {
                        if (TestHostHelper.DetectedTestHost == "VS Code")
                        {
                            Assert.Null(exception);
                            Assert.True(Console.IsInputRedirected);
                        }
                        else
                        {
                            Assert.NotNull(exception);
                            Assert.False(Console.IsInputRedirected);
                        }
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                    }
                    else if (runningUnderServer && TestHostHelper.DetectedTestHost == "Unknown" && TestHostHelper.IsWsl)
                    {
                        Assert.Null(exception);
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        Assert.True(Console.IsInputRedirected);
                    }
                    else if (runningUnderServer)
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
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
                    if (runningUnderDotnetTestPipe)
                    {
                        if (TestHostHelper.DetectedTestHost == "Unknown" || 
                            TestHostHelper.DetectedTestHost == "VS Code (TERM_PROGRAM)")
                        {
                            Assert.Null(exception);
                        }
                        else
                        {
                            Assert.NotNull(exception);
                        }
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                    else if (runningUnderReSharper)
                    {
                        if (TestHostHelper.DetectedTestHost == "VS Code")
                        {
                            Assert.Null(exception);
                        }
                        else
                        {
                            Assert.NotNull(exception);
                        }
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        Assert.True(Console.IsInputRedirected);
                    }
                    else if (runningUnderServer && TestHostHelper.DetectedTestHost == "Unknown" && TestHostHelper.IsWsl)
                    {
                        Assert.Null(exception);
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                    else if (runningUnderServer)
                    {
                        if (TestHostHelper.DetectedTestHost == "VS Code")
                        {
                            Assert.Null(exception);
                            Assert.True(Console.IsInputRedirected);
                        }
                        else
                        {
                            Assert.NotNull(exception);
                            Assert.False(Console.IsInputRedirected);
                        }
                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                    }
                    // else if (isCi)
                    // {
                    //     Assert.Null(exception);
                    //     Assert.False(Console.IsOutputRedirected);
                    //     Assert.False(Console.IsErrorRedirected);
                    //     Assert.False(Console.IsInputRedirected);
                    // }
                    else
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                if (isAttachedToDebugger)
                {
                    if (runningUnderDotnetTestPipe)
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                    else if (runningUnderReSharper)
                    {
                        if (TestHostHelper.DetectedTestHost == "VS Code (VSCODE_PID)")
                        {
                            Assert.Null(exception);
                            Assert.True(Console.IsInputRedirected);
                        }
                        else
                        {
                            Assert.NotNull(exception);
                            Assert.False(Console.IsInputRedirected);
                        }

                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                    }
                    else if (runningUnderServer)
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
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
                    if (runningUnderDotnetTestPipe)
                    {
                        if (TestHostHelper.DetectedTestHost == "Unknown" ||
                            TestHostHelper.DetectedTestHost == "VS Code (TERM_PROGRAM)")
                        {
                            Assert.Null(exception);
                        }
                        else
                        {
                            Assert.NotNull(exception);
                        }

                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
                    else if (runningUnderReSharper)
                    {
                        if (TestHostHelper.DetectedTestHost == "VS Code (VSCODE_PID)")
                        {
                            Assert.Null(exception);
                        }
                        else
                        {
                            Assert.NotNull(exception);
                        }

                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                        Assert.True(Console.IsInputRedirected);
                    }
                    else if (runningUnderServer)
                    {
                        if (TestHostHelper.DetectedTestHost == "VS Code (VSCODE_PID)")
                        {
                            Assert.Null(exception);
                            Assert.True(Console.IsInputRedirected);
                        }
                        else
                        {
                            Assert.NotNull(exception);
                            Assert.False(Console.IsInputRedirected);
                        }

                        Assert.True(Console.IsOutputRedirected);
                        Assert.True(Console.IsErrorRedirected);
                    }
                    // else if (isCi)
                    // {
                    //     Assert.Null(exception);
                    //     Assert.False(Console.IsOutputRedirected);
                    //     Assert.False(Console.IsErrorRedirected);
                    //     Assert.False(Console.IsInputRedirected);
                    // }
                    else
                    {
                        Assert.Null(exception);
                        Assert.False(Console.IsOutputRedirected);
                        Assert.False(Console.IsErrorRedirected);
                        Assert.False(Console.IsInputRedirected);
                    }
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
    }
}