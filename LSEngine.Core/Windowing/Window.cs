namespace LSEngine.Core.Windowing
{
    using Silk.NET.Maths;
    using Silk.NET.OpenGL;
    using Silk.NET.Windowing;
    using Silk.NET.Input;
    using System;
    using Silk.NET.SDL;
    using System.Runtime.CompilerServices;

    public class Window
    {
        private int Width { get; init; }
        private int Height { get; init; }
        private string Title { get; init; }

        private IWindow? SilkNetWindow { get; set; }

        private static GL Gl;

        private static uint Vbo;
        private static uint Ebo;
        private static uint Vao;

        private static uint Shader;

        // Default shaders
        private string vertShader = @"
        #version 330 core //Using version GLSL version 3.3
        layout (location = 0) in vec4 vPos;
        out vec4 color;
        void main()
        {
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
            color = vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";


        private string fragShader = @"
        #version 330 core
        in vec4 color;
        out vec4 FragColor;

        void main()
        {
            FragColor = color;
        }
        ";
        //Vertex data, uploaded to the VBO.
        private static readonly float[] Vertices =
        {
            //X    Y      Z     
             0.5f,  0.5f, 0.0f,
             0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.0f
        };

        //Index data, uploaded to the EBO.
        private static readonly uint[] Indices =
        {
            0, 1, 3,
            1, 2, 3
        };


        public Window(int width = 1200, int height = 800, string title = "LSEngine Default Window")
        {
            Width = width;
            Height = height;
            Title = title;
        }

        public void Init(string vertShader = null, string fragShader = null)
        {

            if(vertShader != null) { this.vertShader = vertShader; }
            if(fragShader != null) { this.fragShader = fragShader; }

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(Width, Height);
            options.Title = Title;
        
            this.SilkNetWindow = Silk.NET.Windowing.Window.Create(options);
            SilkNetWindow.Load += OnLoad;
            SilkNetWindow.Render += OnRender;
            SilkNetWindow.Update += OnUpdate;
            SilkNetWindow.Closing += OnClose;
        }

        private void OnClose()
        {
            //Remember to delete the buffers.
            Gl.DeleteBuffer(Vbo);
            Gl.DeleteBuffer(Ebo);
            Gl.DeleteVertexArray(Vao);
            Gl.DeleteProgram(Shader);
        }

        private void OnUpdate(double obj)
        {
        }

        private unsafe void OnRender(double obj)
        {
            // Clear color channel

            Gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            // Bind geometry and shaders
            Gl.BindVertexArray(Vao);
            Gl.UseProgram(Shader);

            // Draw
            Gl.DrawElements(PrimitiveType.Triangles, (uint) Indices.Length, DrawElementsType.UnsignedInt, null);
        }

        private unsafe void OnLoad()
        {
            IInputContext inputCtx = SilkNetWindow.CreateInput();
            for (int i = 0; i < inputCtx.Keyboards.Count; i++)
            {
                inputCtx.Keyboards[i].KeyDown += KeyDown;
            }

            Gl = GL.GetApi(SilkNetWindow);

            // Vao
            Vao = Gl.GenVertexArray();
            Gl.BindVertexArray(Vao);

            // Vbo
            Vbo = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo);
            fixed (void* v = &Vertices[0])
            {
                Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(uint)), v, BufferUsageARB.StaticDraw);
            }
            // Ebo
            Ebo = Gl.GenBuffer(); //Creating the buffer.
            Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo); //Binding the buffer.
            fixed (void* i = &Indices[0])
            {
                Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(Indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw); //Setting buffer data.
            }


            //Creating a vertex shader.
            uint vertexShader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vertexShader, vertShader);
            Gl.CompileShader(vertexShader);

            //Checking the shader for compilation errors.
            string infoLog = Gl.GetShaderInfoLog(vertexShader);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                Console.WriteLine($"Error compiling vertex shader {infoLog}");
            }

            //Creating a fragment shader.
            uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fragmentShader, fragShader);
            Gl.CompileShader(fragmentShader);


            //Checking the shader for compilation errors.
            infoLog = Gl.GetShaderInfoLog(fragmentShader);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                Console.WriteLine($"Error compiling fragment shader {infoLog}");
            }

            //Combining the shaders under one shader program.
            Shader = Gl.CreateProgram();
            Gl.AttachShader(Shader, vertexShader);
            Gl.AttachShader(Shader, fragmentShader);
            Gl.LinkProgram(Shader);

            //Checking the linking for errors.
            Gl.GetProgram(Shader, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(Shader)}");
            }


            //Delete the no longer useful individual shaders;
            Gl.DetachShader(Shader, vertexShader);
            Gl.DetachShader(Shader, fragmentShader);
            Gl.DeleteShader(vertexShader);
            Gl.DeleteShader(fragmentShader);

            //Tell opengl how to give the data to the shaders.
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
            Gl.EnableVertexAttribArray(0);

        }

        private void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.Escape)
            {
                SilkNetWindow.Close();
            }
        }

        public void Show()
        {
            SilkNetWindow.Run();
            SilkNetWindow.Dispose();
        }
    }
}