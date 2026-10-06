using HeadlessTestEnvironmentUsingXunitV3;

NetDriver consoleDriver = new();
consoleDriver.Init();
Console.WriteLine($"Driver: {consoleDriver.GetType().Name}");
Console.WriteLine($"Console Width: {consoleDriver.Width}, Height: {consoleDriver.Height}");