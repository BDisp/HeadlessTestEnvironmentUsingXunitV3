using System.Runtime.CompilerServices;

namespace HeadlessTestEnvironmentUsingXunitV3.Tests
{
    public static class TestHostInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Ensure TestHostHelper is initialized as soon as the test assembly is loaded.
            // This runs once per test assembly load (i.e., each test run).
            try
            {
                TestHostHelper.Initialize();
            }
            catch
            {
                // Swallow exceptions to avoid failing test discovery/initialization.
            }
        }
    }
}