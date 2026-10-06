using System.Drawing;

namespace HeadlessTestEnvironmentUsingXunitV3
{
    public abstract class ConsoleDriver : IConsoleDriver
    {
        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public Size DefaultSize { get; protected set; } = new(80, 25);
        public bool IsOutputRedirected { get; protected set; }
        public bool IsErrorRedirected { get; protected set; }
        public bool IsInputRedirected { get; protected set; }

        public abstract void Init();
    }
}