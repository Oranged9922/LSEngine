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
                SceneSettings s = new();
                scene.SetSceneSettings(s);
                scene.Run();
            }
        }
    }

    class SceneSettings
    {
        public CameraSettings CameraSettings { get; set; } = new();
    }


    public class CameraSettings
    {
        public float MinRenderDistance { get; set; } = 0.1f;
        public float MaxRenderDistance { get; set; } = 5000f;
        public float CameraSpeed { get; set; } = 0.1f;
    }
}
