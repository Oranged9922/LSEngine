using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace LSEngine
{
    public static class Program
    {
        private static void Main()
        {
            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(1920,1080),
                Title = "LSEngine - Sponza",
            };

            using (var scene = new Scene(GameWindowSettings.Default, nativeWindowSettings))
            {
                scene.Run();
            }
        }
    }
}
