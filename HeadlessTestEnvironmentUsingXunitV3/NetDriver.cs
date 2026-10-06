namespace HeadlessTestEnvironmentUsingXunitV3
{
    public class NetDriver : ConsoleDriver
    {
        public override void Init()
        {
            // Initialization logic for the console driver
            try
            {
                Width = Console.WindowWidth;
                Height = Console.WindowHeight;
            }
            catch (IOException)
            {
                // Handle the case where the console is not available
                Width = DefaultSize.Width; // Default width
                Height = DefaultSize.Height; // Default height
            }
            finally
            {
                // Capture the redirection states
                IsOutputRedirected = Console.IsOutputRedirected;
                IsErrorRedirected = Console.IsErrorRedirected;
                IsInputRedirected = Console.IsInputRedirected;
            }
        }
    }
}