using LSEngine.Core.Windowing;

namespace LSEngine.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Window w = new Window();

            w.Init();
            w.Show();
        }
    }
}