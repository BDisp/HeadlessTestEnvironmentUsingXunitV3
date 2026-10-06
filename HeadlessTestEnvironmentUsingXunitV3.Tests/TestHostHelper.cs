using System.Diagnostics;
#if WINDOWS
using System.Management;
#endif
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace HeadlessTestEnvironmentUsingXunitV3.Tests
{
    public static class TestHostHelper
    {
        public static bool RunOnce;
        public static string? ProcessExe { get; private set; }
        public static string? AssemblyLocation { get; private set; }
        public static string? EntryAssembly { get; private set; }
        public static string? AppDomainBaseDirectory { get; private set; }
        public static string? EnvironmentCurrentDirectory { get; private set; }
        public static string? CommandLine { get; private set; }
        public static string? DetectedTestHost { get; private set; }

        public static void Initialize()
        {
            if (RunOnce)
            {
                return;
            }

            RunOnce = true;
            // Print runtime diagnostics so you can compare Test Explorer / dotnet test vs F5 runs
            ProcessExe = Process.GetCurrentProcess().MainModule?.FileName;
            Console.WriteLine("Process exe: " + ProcessExe);
            AssemblyLocation = Assembly.GetExecutingAssembly().Location;
            Console.WriteLine("Assembly location: " + AssemblyLocation);
            EntryAssembly = Assembly.GetEntryAssembly()?.Location ?? "<null>";
            Console.WriteLine("Entry assembly: " + EntryAssembly);
            AppDomainBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine("AppDomain BaseDirectory: " + AppDomainBaseDirectory);
            EnvironmentCurrentDirectory = Environment.CurrentDirectory;
            Console.WriteLine("Environment.CurrentDirectory: " + EnvironmentCurrentDirectory);
            CommandLine = Environment.CommandLine;
            Console.WriteLine("Command line: " + CommandLine);
            DetectedTestHost = DetectTestHost();
            Console.WriteLine("Detected Test Host: " + DetectedTestHost);

            // Also persist the same diagnostics to files so any test host can be inspected.
            try
            {
                string[] lines = new[]
                {
                    "Process exe: " + ProcessExe, "Assembly location: " + AssemblyLocation,
                    "Entry assembly: " + EntryAssembly, "AppDomain BaseDirectory: " + AppDomainBaseDirectory,
                    "Environment.CurrentDirectory: " + EnvironmentCurrentDirectory, "Command line: " + CommandLine,
                    "Detected Test Host: " + DetectedTestHost
                };

                string id = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "_" + Guid.NewGuid().ToString("N");

                // Candidate locations to write diagnostics where different test hosts will be able to write/read
                string[] targetDirs = new[]
                {
                    AppDomain.CurrentDomain.BaseDirectory
                    //Environment.CurrentDirectory,
                    //Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory,
                    //Path.GetTempPath()
                };

                foreach (string dir in targetDirs.Distinct())
                {
                    try
                    {
                        if (string.IsNullOrEmpty(dir))
                        {
                            continue;
                        }

                        Directory.CreateDirectory(dir);
                        string file = Path.Combine(dir, $"Diagnostic_PrintProcessInfo_{id}.txt");
                        File.WriteAllLines(file, lines);
                        Console.WriteLine("Wrote diagnostics to: " + file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unable to write diagnostics to '{dir}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Diagnostic write failed: " + ex);
            }

            // Keep test passing; the output and files are the diagnostic information
            // Detect if likely running from Visual Studio or VS Code and print that too
            string DetectTestHost()
            {
                try
                {
                    // Quick environment checks that common VS Code indicators set in spawned processes
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_PID")))
                    {
                        return "VS Code (VSCODE_PID)";
                    }

                    if (Environment.GetEnvironmentVariable("TERM_PROGRAM") == "vscode")
                    {
                        return "VS Code (TERM_PROGRAM)";
                    }

                    // Walk parent process chain and inspect command lines on each platform
                    int pid = Environment.ProcessId;
                    while (true)
                    {
                        int parentId = 0;

#if WINDOWS
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            try
                            {
                                using ManagementObjectSearcher searcher =
                                    new($"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {pid}");
                                foreach (ManagementBaseObject mbo in searcher.Get())
                                {
                                    parentId = Convert.ToInt32(mbo["ParentProcessId"]);
                                    break;
                                }
                            }
                            catch
                            {
                                break;
                            }
                        }
#endif
                        if (parentId == 0 && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                        {
                            try
                            {
                                string statusPath = $"/proc/{pid}/status";
                                if (!File.Exists(statusPath))
                                {
                                    break;
                                }

                                string[] content = File.ReadAllLines(statusPath);
                                string? ppidLine = content.FirstOrDefault(l => l.StartsWith("PPid:"));
                                if (ppidLine == null)
                                {
                                    break;
                                }

                                string[] parts = ppidLine.Split(':', 2);
                                if (parts.Length < 2)
                                {
                                    break;
                                }

                                parentId = int.Parse(parts[1].Trim());
                            }
                            catch
                            {
                                break;
                            }
                        }
                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                        {
                            // macOS: use ps to get parent pid
                            parentId = RunPsGetParent(pid);
                            if (parentId <= 0)
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }

                        if (parentId == 0 || parentId == pid)
                        {
                            break;
                        }

                        string? cmd = null;
                        try
                        {
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                try
                                {
                                    Process parent = Process.GetProcessById(parentId);
                                    cmd = parent.ProcessName;
                                }
                                catch
                                {
                                    cmd = null;
                                }
                            }
                            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                            {
                                string cmdPath = $"/proc/{parentId}/cmdline";
                                if (File.Exists(cmdPath))
                                {
                                    byte[] raw = File.ReadAllBytes(cmdPath);
                                    cmd = Encoding.UTF8.GetString(raw).Replace('\0', ' ').Trim();
                                }
                                else
                                {
                                    cmd = RunPsGetCommand(parentId);
                                }
                            }
                            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                            {
                                cmd = RunPsGetCommand(parentId);
                            }
                        }
                        catch
                        {
                            // ignore
                        }

                        if (!string.IsNullOrEmpty(cmd))
                        {
                            string pname = Path.GetFileName(cmd).ToLowerInvariant();
                            if (pname.Contains("devenv") || pname.Contains("visual"))
                            {
                                return "Visual Studio";
                            }

                            if (pname.Contains("code") || pname.Contains("vscode"))
                            {
                                return "VS Code";
                            }

                            if (pname.Contains("vstest") || pname.Contains("testhost"))
                            {
                                return "VSTest/TestHost";
                            }
                        }

                        pid = parentId;
                    }
                }
                catch
                {
                    // ignore detection failures
                }

                return "Unknown";
            }

            int RunPsGetParent(int pid)
            {
                try
                {
                    string? output = RunPs($"-o ppid= -p {pid}");
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        return -1;
                    }

                    if (int.TryParse(output.Trim(), out int p))
                    {
                        return p;
                    }
                }
                catch
                {
                }

                return -1;
            }

            string? RunPsGetCommand(int pid)
            {
                try
                {
                    string? outp = RunPs($"-p {pid} -o comm=");
                    return outp?.Trim();
                }
                catch
                {
                }

                return null;
            }

            string? RunPs(string args)
            {
                try
                {
                    ProcessStartInfo psi = new("ps", args)
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using Process? p = Process.Start(psi);
                    if (p == null)
                    {
                        return null;
                    }

                    string outp = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(2000);
                    return outp;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}