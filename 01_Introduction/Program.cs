// comment if not wanted
#define _USE_BUFFERS
#define _INTERLEAVED_BUFFER


using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Common;

// use different using if want to change the version
using OpenTK.Graphics.OpenGL4;


var windowSettings = new NativeWindowSettings()
{
    Size = new(800, 600),
    Title = "LSEngine - 01_Introduction"
};


using (var scene = new Scene(GameWindowSettings.Default, windowSettings)) scene.Run();

    GLFWCallbacks.ErrorCallback errorCallback = (OpenTK.Windowing.GraphicsLibraryFramework.ErrorCode error, string description) =>

    {
        Console.WriteLine(
$@"
GLFW Error {error}: 
{description}."
        );
    };


class Scene : GameWindow
{
    #region BaseConstructor
    public Scene(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    { }
    #endregion

    // Vertex shaders
    string[] vsSource =
        {
// Vertex shader with hardcoded triangle coordinates
@"
#version 330 core

vec3 positions[3] = vec3[3](vec3(-0.25f, -0.25f, 0.0f),
                            vec3( 0.25f, -0.25f, 0.0f),
                            vec3( 0.25f,  0.25f, 0.0f));

vec3 colors[3] = vec3[3](vec3(1.0f, 0.0f, 0.0f),
                         vec3(0.0f, 1.0f, 0.0f),
                         vec3(0.0f, 0.0f, 1.0f));

out vec3 vColor;

void main()
{
  vColor = colors[gl_VertexID].rgb;
  gl_Position = vec4(positions[gl_VertexID].xyz, 1.0f);
}
"
,
// Vertex shader accepting positions and colors from a buffer
@"
#version 330 core

layout (location = 0) in vec3 position;
layout (location = 1) in vec3 color;

out vec3 vColor;

void main()
{
  vColor = color;
  gl_Position = vec4(position.xyz, 1.0f);
}
"
        };
    // Fragment shaders
    string[] fsSource =
    {
@"
#version 330 core

in vec3 vColor;
out vec4 color;

void main()
{
  color = vec4(vColor.rgb, 1.0f);
}
"
,
""
    };

    // Max buffer length
    const int maxBufferLength = 256;

    // Shader program ID
    int shaderProgramID = 0;

    // Vertex Array Object
    int vaoID = 0;

#if _USE_BUFFERS
#if _INTERLEAVED_BUFFER
    // Vertex buffer for holding data
    int vertexBuffer = 0;
#else
    // Buffer holding positional data
    int positionBuffer = 0;
    // Buffer holding color data
    int colorBuffer = 0;
#endif // _INTERLEAVED_BUFFER
#endif // _USE_BUFFERS


    bool CompileShaders()
    {
        int vertShader, fragShader;
        int vsIndex = 0;
        int res;
#if _USE_BUFFERS
        vsIndex = 1;
#endif

        // Create and compile shader
        vertShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertShader, vsSource[vsIndex]);
        GL.CompileShader(vertShader);

        // check if compilation was successful
        Console.WriteLine("Vertex shader compilation:");
        GL.GetShader(vertShader, ShaderParameter.CompileStatus, out res);
        if (res == 0)
        {
            Console.WriteLine(GL.GetShaderInfoLog(vertShader));
            return false;
        }
        else
        {
            Console.WriteLine("OK");
            Console.WriteLine("--------------------------------------------------------------------------------");
        }

        // create and compile fragment shader
        fragShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragShader, fsSource[0]);
        GL.CompileShader(fragShader);


        // check if compilation was successful
        Console.WriteLine("Fragment shader compilation:");
        GL.GetShader(vertShader, ShaderParameter.CompileStatus, out res);
        if (res == 0)
        {
            Console.WriteLine(GL.GetShaderInfoLog(fragShader));
            return false;
        }
        else
        {
            Console.WriteLine("OK");
            Console.WriteLine("--------------------------------------------------------------------------------");
        }

        // Create the shader program, attach shaders and link
        shaderProgramID = GL.CreateProgram();
        GL.AttachShader(shaderProgramID, vertShader);
        GL.AttachShader(shaderProgramID, fragShader);
        GL.LinkProgram(shaderProgramID);

        // check if linking was successful
        Console.WriteLine("ShaderProgram compilation:");

        GL.GetProgram(shaderProgramID, GetProgramParameterName.LinkStatus, out res);
        if (res == 0)
        {
            Console.WriteLine(GL.GetProgramInfoLog(shaderProgramID));
            return false;
        }
        else
        {
            Console.WriteLine("OK");
            Console.WriteLine("--------------------------------------------------------------------------------");
        }


        // Clean up resources 

        GL.DetachShader(shaderProgramID, vertShader);
        GL.DeleteShader(vertShader);
        GL.DetachShader(shaderProgramID, fragShader);
        GL.DeleteShader(fragShader);

        return true;
    }

    void CreateGeometry()
    {
        vaoID = GL.GenVertexArray();
        GL.BindVertexArray(vaoID);

#if _USE_BUFFERS
        // Data description
        const int vertCount = 6;
        const int posDim = 3;
        const int colorDim = 3;
#if _INTERLEAVED_BUFFER
        // Interleaved buffer with positions and colors
        float[] vertices = {-0.25f, -0.25f, 0.0f, // position of the 1st vertex (XYZ)
                       1.0f,   0.0f,  0.0f, // color of the 1st vertex (RGB)
                       0.25f, -0.25f, 0.0f, // position of the 2nd vertex (XYZ)
                        0.0f,  1.0f,  0.0f, // color of the 2nd vertex (RGB)
                       0.25f,  0.25f, 0.0f, // ...
                        0.0f,  0.0f,  1.0f,
                      -0.25f,  0.25f, 0.0f,
                        1.0f,  0.0f,  1.0f,
                      -0.25f, -0.25f, 0.0f,
                        1.0f,  1.0f,  0.0f,
                       0.25f,  0.25f, 0.0f,
                        0.0f,  1.0f,  1.0f};

        // Generate memory storage
        vertexBuffer = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);

        // Fill the buffer with the data: we're passing 6 vertices each composed of 2 vec3 values => sizeof(float) * 2 * 3 * 6
        // BufferUsageHint.StaticDraw tells the driver that we don't intent to change the buffer afterwards and want only to draw from it
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            sizeof(float) * (posDim + colorDim) * vertCount,
            vertices,
            BufferUsageHint.StaticDraw);


        // Now, we need to tell OpenGL where to find the data using interleaved buffer: X0,Y0,Z0,R0,G0,B0,X1,Y1,Z1,R1,G1,B1,...
        // Stride - how many bytes to skip before the next vertex attribute
        // Offset - how many bytes to skip for the first attribute of that type

        // Positions: 3 floats, stride = 6 (6 floats/vertex), offset = 0
        GL.VertexAttribPointer(0, posDim, VertexAttribPointerType.Float, false, (posDim + colorDim) * sizeof(float), 0);
        GL.EnableVertexAttribArray(0); // Tied to the location in the shader

        // Colors: 3 floats, stride = 6 (6 floats/vertex), offset = 3 floats, i.e., 3 * 4 bytes
        GL.VertexAttribPointer(1, colorDim, VertexAttribPointerType.Float, false, (posDim + colorDim) * sizeof(float), posDim * sizeof(float));
        GL.EnableVertexAttribArray(1); // Tied to the location in the shader

        // Unbind the buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
#else

 // Buffer with positions
  float[] positions = {-0.25f, -0.25f, 0.0f, // position of the 1st vertex (XYZ)
                        0.25f, -0.25f, 0.0f, // position of the 2nd vertex (XYZ)
                        0.25f,  0.25f, 0.0f, // ...
                       -0.25f,  0.25f, 0.0f,
                       -0.25f, -0.25f, 0.0f,
                        0.25f,  0.25f, 0.0f};

  // Buffer with colors
  byte[] colors =          { 255,   0,   0, // color of the 1st vertex (RGB)
                               0, 255,   0, // color of the 2nd vertex (RGB)
                               0,   0, 255, // ...
                             255,   0, 255,
                             255, 255,   0,
                               0, 255, 255};

        // Generate the memory storage
        positionBuffer = GL.GenBuffer();
        colorBuffer = GL.GenBuffer();

        // Fill the buffer with the data: we're passing 6 vertices each composed of vec3 values => sizeof(float) * 3 * 6
        GL.BindBuffer(BufferTarget.ArrayBuffer, positionBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * posDim * vertCount, positions, BufferUsageHint.StaticDraw);


        int attribIndex = GL.GetAttribLocation(shaderProgramID, "position");

        // Positions: 3 floats, stride = 3 (3 floats/vertex), offset = 0
        GL.VertexAttribPointer(attribIndex, posDim, VertexAttribPointerType.Float, false, posDim * sizeof(float), 0);
        GL.EnableVertexAttribArray(attribIndex); // Tied to the location in the shader

        // Unbind the buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer,0);

        // Fill the buffer with the data: we're passing 6 vertices each composed of vec3 values => sizeof(char) * 3 * 6
        GL.BindBuffer(BufferTarget.ArrayBuffer, colorBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, sizeof(byte) * colorDim * vertCount, colors, BufferUsageHint.StaticDraw);



        attribIndex = GL.GetAttribLocation(shaderProgramID, "color");

        // Colors: 3 chars, stride = 3 (3 char/vertex), offset = 0
        GL.VertexAttribPointer(attribIndex, colorDim,VertexAttribPointerType.Byte,true, colorDim * sizeof(byte), 0);
        GL.EnableVertexAttribArray(attribIndex); // Tied to the location in the shader

        // Unbind the buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
#endif // _INTERLEAVED_BUFFER
#endif // _USE_BUFFERS
    }


    // Run immediately after Run() is called
    protected override void OnLoad()
    {
        base.OnLoad();
        Console.WriteLine(
            $"Graphics card used: {GL.GetString(StringName.Vendor)}, " +
            $"GL version: {GL.GetString(StringName.Version)}");
        Init();
    }

    void Init()
    {
        if (!CompileShaders())
        {
            Console.WriteLine("Failed to compile shaders!");
            Close();
            return;
        }

        CreateGeometry();
    }

    void ProcessInput()
    {
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    void RenderScene()
    {
        // Clear the color buffer
        GL.ClearColor(0.1f, 0.2f, 0.4f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // Tell OpenGL we'd like to use the previously compiled shader program
        GL.UseProgram(shaderProgramID);

        // Draw the scene geometry - just tell OpenGL we're drawing at this point
        GL.PointSize(10.0f);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        // Unbind the shader program
        GL.UseProgram(0);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        ProcessInput();
        RenderScene();
        SwapBuffers();
    }
}


