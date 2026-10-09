System.Console behavior under xUnit v3 and common test hosts
===========================================================

This project contains diagnostics and helpers to observe how System.Console behaves when unit tests run under various runners and IDEs. This README summarizes typical behavior you can expect on Windows, Linux, and macOS when tests are started from:

- A terminal (dotnet test / running the compiled test executable)
- Visual Studio 2026 (Test Explorer / F5 / Ctrl+F5)
- VS Code (Run Tests or the built-in test UI)
- ReSharper test runner / extension
- Microsoft Test Explorer (Visual Studio)
- ReSharper Test Explorer

Short summary
-------------

- Test runners and IDE test-execution hosts commonly redirect standard input/output/error. When redirected, Console.IsOutputRedirected / Console.IsInputRedirected / Console.IsErrorRedirected return true and Console.WindowWidth/WindowHeight may be unavailable or not reflect a real terminal. Accessing WindowWidth/WindowHeight may throw in some environments — guard with try/catch.
- Running tests inside an IDE's Test Explorer or a test host (testhost) usually means a headless environment: expect default sizes and redirected I/O.
- Running tests from a terminal (dotnet test launched from a shell, or running the test assembly directly) usually provides a real console where WindowWidth/WindowHeight reflect the terminal size and Console.IsOutputRedirected is false (unless the terminal itself redirected output).
- Visual Studio when running tests from Test Explorer uses a test host process and typically redirects console output; F5/Ctrl-F5 runs the executable under devenv and attaches a debugger and a real console for console apps.
- VS Code typically spawns dotnet test or its test host; the test host may be redirected. The editor may set environment variables (VSCODE_PID, TERM_PROGRAM) that you can inspect.

Behavior by runner / tool
-------------------------

Terminal (dotnet test / running exe)
- dotnet test (without special GUI host) usually launches testhost which captures test output and writes it to the terminal. In many configurations Console.IsOutputRedirected will be false for the console process you see, but the test host process may redirect output. When running tests directly in a terminal you can expect Console.WindowWidth and WindowHeight to be available most of the time.

Visual Studio 2026 (Test Explorer)
- Visual Studio uses the VSTest infrastructure and will usually run tests in a separate test host process. That host often redirects standard streams and provides no real interactive console. Console.WindowWidth/Height will often fall back to defaults. Test output is captured and shown in the Test Explorer output window.

Visual Studio (F5 / Run)
- Running the project (F5/Ctrl-F5) launches the app under the debugger or standalone — a real console is present for console apps (on Windows) and WindowWidth/Height are readable. This is not the same code path as Test Explorer.

VS Code
- VS Code's test integrations normally call dotnet test or a test adapter. The editor may set environment variables like VSCODE_PID or TERM_PROGRAM=vscode which can be used as signals. The actual behavior depends on whether tests are run via an integrated terminal or the test adapter; output might be captured and I/O redirected.

ReSharper test runner / ReSharper Test Explorer
- ReSharper runs tests through its own runner process (or via dotnet test depending on configuration). The runner commonly captures console output and runs tests in a separate process, so console I/O is often redirected.

Executable (running test assembly directly)
- If you launch the test assembly or a small harness directly in a terminal, you will generally have a real console (unless you explicitly pipe/redirect). WindowWidth/WindowHeight are available.

Platform-specific notes
-----------------------

Windows
- Full process tree inspection is available (WMI / System.Management). Test hosts like testhost.* or Visual Studio (devenv) parent processes can be detected by process name. xUnit tests running under the Visual Studio Test Explorer typically execute in a test-host process with redirected I/O.

Linux
- Process information is available in /proc/<pid> (status and cmdline). Many CI/test adapters redirect output. macOS does not have /proc, so ps is typically used to query parent and command.

macOS
- Use ps to inspect parent pid and the command name. Many test runners behave like on Linux — the test host may redirect I/O.

Where to write diagnostics so test hosts can read them
----------------------------------------------------

- AppDomain.CurrentDomain.BaseDirectory is usually writable by the test host and is a good place to write per-run diagnostics.
- Path.GetTempPath() is another portable option (tmp on Unix, Temp on Windows).
- Avoid relying on Environment.CurrentDirectory as hosts may set it to different values; prefer BaseDirectory or an explicit temp path.

Recommended detection & defensive coding
--------------------------------------

- Use Console.IsOutputRedirected / Console.IsErrorRedirected / Console.IsInputRedirected to detect redirection and guard behavior.
- Wrap Console.WindowWidth/WindowHeight access in try/catch and fall back to a sensible default (e.g., 80x25) when unavailable.
- If you need to know the environment that launched the test, inspect common environment variables (Visual Studio, VSCODE_PID, TERM_PROGRAM) and, when feasible, walk the parent process chain and inspect process names/command lines to look for "devenv", "Code", "testhost", etc. The test project contains a TestHostHelper that implements this kind of detection in a cross-platform way.
- Prefer using xUnit's ITestOutputHelper for per-test output. Console.WriteLine is still captured by the test host but may not appear where you expect in an IDE.

Implementation note
-------------------

This repository includes a TestHostHelper.Initialize() helper that collects diagnostics (process exe, assembly location, command line, base directory) and tries to detect the launching host using environment variables and a cross-platform parent-process inspection strategy (WMI on Windows, /proc on Linux, ps on macOS). A module initializer in the test assembly calls this Initialize method automatically before tests run so you can inspect the collected values from any test.

Summary
-------

Treat test execution environments as headless by default: do not rely on Console.WindowWidth/Height or interactive input in tests. Detect redirection with Console.IsOutputRedirected and fall back to defaults. Use AppDomain BaseDirectory or temp to store diagnostics. When you need to detect IDE/runner, combine environment-variable checks with parent-process inspection; the TestHostHelper in this repository provides an example.

If you want a shorter checklist or a sample snippet to detect hosts, see the TestHostHelper class in the test project.

Platform vs test-host behavior table
-----------------------------------

<table>
	<caption><strong>AttachedToDebugger</strong> (debugger / interactive terminal)</caption>
  <thead>
	<tr>
	  <th>OS</th>
	  <th>Command / runner</th>
	  <th>WindowWidth Exception</th>
	  <th>IsOutputRedirected</th>
	  <th>IsErrorRedirected</th>
	  <th>IsInputRedirected</th>
	  <th>Observation</th>
	</tr>
  </thead>
  <tbody>
	<tr><td>Windows</td><td>--dotnet-test-pipe</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>Windows</td><td>ReSharperTestRunner.dll</td><td>not null</td><td>true</td><td>true</td><td>false</td><td></td></tr>
	<tr><td>Windows</td><td>ReSharperTestRunner.dll</td><td>not null</td><td>true</td><td>true</td><td>true</td><td>VS Code (VSCODE_PID)</td></tr>
	<tr><td>Windows</td><td>--server</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>Windows</td><td>dotnet run/F5/Ctrl+F5</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>Linux</td><td>--dotnet-test-pipe</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>Linux</td><td>ReSharperTestRunner.dll</td><td>not null</td><td>true</td><td>true</td><td>false</td><td></td></tr>
	<tr><td>Linux</td><td>ReSharperTestRunner.dll</td><td>null</td><td>true</td><td>true</td><td>true</td><td>VS Code</td></tr>
	<tr><td>Linux</td><td>--server</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>Linux</td><td>--server</td><td>null</td><td>true</td><td>true</td><td>true</td><td>Unknown & IsWsl</td></tr>
	<tr><td>Linux</td><td>dotnet run/F5/Ctrl+F5</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>macOS</td><td>--dotnet-test-pipe</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>macOS</td><td>ReSharperTestRunner.dll</td><td>not null</td><td>true</td><td>true</td><td>false</td><td></td></tr>
	<tr><td>macOS</td><td>ReSharperTestRunner.dll</td><td>null</td><td>true</td><td>true</td><td>true</td><td>VS Code (VSCODE_PID)</td></tr>
	<tr><td>macOS</td><td>--server</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>macOS</td><td>dotnet run/F5/Ctrl+F5</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>Others</td><td></td><td>null</td><td>false</td><td>false</td><td>false</td><td>Values may vary</td></tr>
</tbody>
</table>

<table>
	<caption><strong>Not AttachedToDebugger</strong> (headless / test-host)</caption>
  <thead>
	<tr>
	  <th>OS</th>
	  <th>Command / runner</th>
	  <th>WindowWidth Exception</th>
	  <th>IsOutputRedirected</th>
	  <th>IsErrorRedirected</th>
	  <th>IsInputRedirected</th>
	  <th>Observation</th>
	</tr>
  </thead>
  <tbody>
	<tr><td>Windows</td><td>--dotnet-test-pipe</td><td>not null</td><td>true</td><td>true</td><td>false</td><td></td></tr>
	<tr><td>Windows</td><td>--dotnet-test-pipe</td><td>not null</td><td>true</td><td>true</td><td>true</td><td>isCi</td></tr>
	<tr><td>Windows</td><td>ReSharperTestRunner.dll</td><td>not null</td><td>true</td><td>true</td><td>true</td><td></td></tr>
	<tr><td>Windows</td><td>--server</td><td>not null</td><td>true</td><td>true</td><td>false</td><td></td></tr>
	<tr><td>Windows</td><td>--server</td><td>null</td><td>false</td><td>false</td><td>false</td><td>VS Code (VSCODE_PID)</td></tr>
	<tr><td>Windows</td><td>dotnet run/F5/Ctrl+F5</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>Linux</td><td>--dotnet-test-pipe</td><td>not null</td><td>true</td><td>true</td><td>false</td><td></td></tr>
	<tr><td>Linux</td><td>--dotnet-test-pipe</td><td>null</td><td>true</td><td>true</td><td>false</td><td>Unknown | VS Code (TERM_PROGRAM)</td></tr>
	<tr><td>Linux</td><td>--dotnet-test-pipe</td><td>not null</td><td>true</td><td>true</td><td>true</td><td>isCi</td></tr>
	<tr><td>Linux</td><td>ReSharperTestRunner.dll</td><td>not null</td><td>true</td><td>true</td><td>true</td><td></td></tr>
	<tr><td>Linux</td><td>ReSharperTestRunner.dll</td><td>null</td><td>true</td><td>true</td><td>true</td><td>VS Code</td></tr>
	<tr><td>Linux</td><td>--server</td><td>not null</td><td>true</td><td>true</td><td>false</td><td></td></tr>
	<tr><td>Linux</td><td>--server</td><td>null</td><td>true</td><td>true</td><td>true</td><td>Unknown & IsWsl</td></tr>
	<tr><td>Linux</td><td>--server</td><td>null</td><td>true</td><td>true</td><td>true</td><td>VS Code</td></tr>
	<tr><td>Linux</td><td>dotnet run/F5/Ctrl+F5</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>macOS</td><td>--dotnet-test-pipe</td><td>not null</td><td>true</td><td>true</td><td>false</td><td></td></tr>
	<tr><td>macOS</td><td>--dotnet-test-pipe</td><td>null</td><td>true</td><td>true</td><td>false</td><td>Unknown | VS Code (TERM_PROGRAM)</td></tr>
	<tr><td>macOS</td><td>--dotnet-test-pipe</td><td>not null</td><td>true</td><td>true</td><td>true</td><td>isCi</td></tr>
	<tr><td>macOS</td><td>ReSharperTestRunner.dll</td><td>not null</td><td>true</td><td>true</td><td>true</td><td></td></tr>
	<tr><td>macOS</td><td>ReSharperTestRunner.dll</td><td>null</td><td>true</td><td>true</td><td>true</td><td>VS Code (VSCODE_PID)</td></tr>
	<tr><td>macOS</td><td>--server</td><td>not null</td><td>true</td><td>true</td><td>false</td><td></td></tr>
	<tr><td>macOS</td><td>--server</td><td>null</td><td>true</td><td>true</td><td>true</td><td>VS Code (VSCODE_PID)</td></tr>
	<tr><td>macOS</td><td>dotnet run/F5/Ctrl+F5</td><td>null</td><td>false</td><td>false</td><td>false</td><td></td></tr>
	<tr><td>Others</td><td></td><td>null</td><td>false</td><td>false</td><td>false</td><td>Values may vary</td></tr>
  </tbody>
</table>

Notes:
- The first table (AttachedToDebugger) reflects expectation when a debugger or interactive terminal is present (no exception accessing WindowWidth/Height and streams not redirected).
- The second table (Not AttachedToDebugger) reflects typical headless/test-host behavior: Console.WindowWidth may throw (tests should guard) and streams are commonly redirected.
- In both tables, use the Observation cell only for explicit conditional signals (e.g., VS Code variables, IsWsl, isCi); leave generic/else cases blank.
- Use Console.IsOutputRedirected / Console.IsErrorRedirected / Console.IsInputRedirected to detect redirected streams at runtime and fall back to defaults for window size.

