using System.Drawing;

namespace HeadlessTestEnvironmentUsingXunitV3
{
    public interface IConsoleDriver
    {
        int Width { get; }
        int Height { get; }
        Size DefaultSize { get; }
        bool IsOutputRedirected { get; }
        bool IsErrorRedirected { get; }
        bool IsInputRedirected { get; }

        void Init();
    }
}