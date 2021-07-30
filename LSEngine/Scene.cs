//#define DebugPrints
#define PrintFPS

using System;
using LSEngine.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace LSEngine
{
   
    public class Scene : GameWindow
    {
        public Scene(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        internal void SetSceneSettings(SceneSettings s)
        {
            this.SceneSettings = s;
            cam.MoveSpeed = s.CameraSettings.CameraSpeed;
            this.MinRenderDistance = s.CameraSettings.MinRenderDistance;
            this.MaxRenderDistance = s.CameraSettings.MaxRenderDistance;
        }
        #region Fields
        Vector3[] vertdata;
        Vector3[] coldata;
        Vector2[] texcoorddata;
        Vector3[] normdata;

        SceneSettings SceneSettings;
        int[] indicedata;
        int ibo_elements;
        int vao;
        int depthMapFBO;
        int depthMap;


        // if dynamically adding objects, check references of this boolean, you might want to change the use of it.
        public bool objectsListHasChanged = true;

        float MinRenderDistance, MaxRenderDistance;
        List<RenderObject> objects = new();
        Camera cam = new();
        List<Light> lights = new();
        const int MAX_LIGHTS = 5;
        Vector2 lastMousePos = new();
        Matrix4 view = Matrix4.Identity;

        Dictionary<string, int> textures = new();
        Dictionary<string, ShaderProgram> shaders = new();
        List<string> shadersList = new();
        string activeShader = "normal";
        int currentShaderIndex = 0;
        Dictionary<String, Material> materials = new();

        float time = 0.0f;
        float dtime = 0.0f;
        int refreshCounter = 0;
        const int maxRefresh = 60;
        #endregion

        protected override void OnLoad()
        {
            base.OnLoad();
            InitializeProgram();
            //Title = "LSEngine";
            GL.ClearColor(Color4.Gray);
            GL.Enable(EnableCap.DepthTest);
            PrintControls();
            Console.WriteLine("Graphics card used: " + GL.GetString(StringName.Vendor) + ", GL version:" +  GL.GetString(StringName.Version));
        }

        private void InitializeProgram()
        {
            lastMousePos = new Vector2(MouseState.X, MouseState.Y);
            CursorVisible = false;
            CursorGrabbed = true;
            cam.MouseSensitivity = 0.025f;
            cam.MoveSpeed = 2;
            ibo_elements = GL.GenBuffer();
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            depthMapFBO = GL.GenFramebuffer();
            depthMap = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, depthMap);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, 1024, 1024, 0, OpenTK.Graphics.OpenGL4.PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);


            // attach depth texture as FBO's depth buffer
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, depthMap, 0);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            LoadResources();

            activeShader = "lit_advanced";
            SetupScene();
            cam.Position += new Vector3(0.0f, 1.0f, 0.0f);
        }

        private void LoadResources()
        {
            // Load Materials and Textures
            LoadMaterials("Materials/sponza.mtl");
            shaders.Add("normal_map", new("Shaders/vs_norm.glsl", "Shaders/fs_norm.glsl", true));
            shadersList.Add("normal_map");
            shaders.Add("simple_shadow", new("Shaders/vs_simple_shadow.glsl", "Shaders/fs_simple_shadow.glsl", true));
            shadersList.Add("simple_shadow");
            shaders.Add("lit_advanced", new("Shaders/vs_lit.glsl", "Shaders/fs_lit_advanced.glsl", true));
            shadersList.Add("lit_advanced");
            shaders.Add("poor_mans_zBuffer", new("Shaders/vs_zBuffer.glsl", "Shaders/fs_zBuffer.glsl", true));
            shadersList.Add("poor_mans_zBuffer");
            shaders.Add("simpleDepthShader", new("Shaders/vs_simpleDepthShader.glsl", "Shaders/fs_simpleDepthShader.glsl", true));
            shadersList.Add("simpleDepthShader");
            shaders.Add("shaderShadow", new("Shaders/vs_shaderShadow.glsl", "Shaders/fs_shaderShadow.glsl", true));
            shadersList.Add("shaderShadow");

        }

        private void LoadMaterials(string filename)
        {
            foreach (var mat in Material.LoadFromFile(filename))
            {
                if (!materials.ContainsKey(mat.Key))
                {
                    materials.Add(mat.Key, mat.Value);
                }
            }

            // Load textures
            foreach (Material mat in materials.Values)
            {
                if (File.Exists(mat.AmbientMap) && !textures.ContainsKey(mat.AmbientMap))
                {
                    textures.Add(mat.AmbientMap, LoadImage(mat.AmbientMap));
                }

                if (File.Exists(mat.DiffuseMap) && !textures.ContainsKey(mat.DiffuseMap))
                {
                    textures.Add(mat.DiffuseMap, LoadImage(mat.DiffuseMap));
                }

                if (File.Exists(mat.SpecularMap) && !textures.ContainsKey(mat.SpecularMap))
                {
                    textures.Add(mat.SpecularMap, LoadImage(mat.SpecularMap));
                }

                if (File.Exists(mat.NormalMap) && !textures.ContainsKey(mat.NormalMap))
                {
                    textures.Add(mat.NormalMap, LoadImage(mat.NormalMap));
                }

                if (File.Exists(mat.OpacityMap) && !textures.ContainsKey(mat.OpacityMap))
                {
                    textures.Add(mat.OpacityMap, LoadImage(mat.OpacityMap));
                }
            };
        }

        private int LoadImage(string filename)
        {
            if (File.Exists(filename)) { }
            try
            {
                Bitmap file = new(filename);
                file.MakeTransparent();
                return LoadImage(file);
            }
            catch (FileNotFoundException)
            {
                return -1;
            }
        }

        int LoadImage(Bitmap image)
        {
            int texID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texID);
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            image.UnlockBits(data);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            return texID;
        }

        private void SetupScene()
        {
            ObjRenderObject sponza = ObjRenderObject.LoadFromFile("ObjectFiles/sponza.obj", materials, textures);
            foreach (var part in sponza.Parts)
            {
                objects.Add(part);
            }

            Light sunLight = new(new Vector3(0, 5, 0), new Vector3(1));
            sunLight.Type = LightType.Directional;
            sunLight.ConeAngle = 10f;
            sunLight.Direction = new Vector3(0, 1, 0).Normalized();
            lights.Add(sunLight);


        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            //GL.Viewport(0, 0, Size.X, Size.Y);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            // 1. render depth of scene to texture (from light's perspective)
            // --------------------------------------------------------------
            Matrix4 lightProjection, lightView, lightSpaceMatrix;
            float near_plane = -100.0f, far_plane = 100.5f;
            //lightProjection = glm::perspective(glm::radians(45.0f), (GLfloat)SHADOW_WIDTH / (GLfloat)SHADOW_HEIGHT, near_plane, far_plane); // note that if you use a perspective projection matrix you'll have to change the light position as the current light position isn't enough to reflect the whole scene
            lightProjection = Matrix4.CreateOrthographicOffCenter(-10.0f, 10.0f, -10.0f, 10.0f, near_plane, far_plane);
            lightView = Matrix4.LookAt(lights[0].Position, new(0.0f), new(0.0f, 1.0f, 0.0f));
            lightSpaceMatrix = lightProjection * lightView;
            // render scene from light's point of view

            GL.UseProgram(shaders["simpleDepthShader"].ProgramID);
            GL.UniformMatrix4(shaders[activeShader].GetUniform("lightSpaceMatrix"), false, ref lightSpaceMatrix);
            GL.Viewport(0, 0, 1024, 1024);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, depthMapFBO);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            RenderScene("simpleDepthShader");
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // reset viewport
            GL.Viewport(0, 0, Size.X, Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit| ClearBufferMask.DepthBufferBit);

            // 2. render scene as normal using the generated depth/shadow map  
            // --------------------------------------------------------------
            GL.Viewport(0, 0, Size.X, Size.Y);
            GL.Clear(ClearBufferMask.ColorBufferBit| ClearBufferMask.DepthBufferBit);
            GL.UseProgram(shaders["shaderShadow"].ProgramID);
            Matrix4 projection = Matrix4.CreatePerspectiveOffCenter(-10,10,-10,10,0.1f, 100.0f);
            Matrix4 view = cam.GetViewMatrix();
            // set light uniforms
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, depthMap);
            RenderScene("shaderShadow");
            shaders[activeShader].DisableVertexAttribArrays();
            GL.Disable(EnableCap.Blend);
            GL.Flush();
            SwapBuffers();
        }

        private void RenderScene(string shader)
        {
            GL.UseProgram(shaders[shader].ProgramID);

            shaders[shader].EnableVertexAttribArrays();

            int indiceat = 0;

            // Draw all objects
            foreach (RenderObject v in objects)
            {


                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                GL.Enable(EnableCap.Blend);
                GL.BindTexture(TextureTarget.Texture2D, v.TextureID);

                GL.UniformMatrix4(shaders[activeShader].GetUniform("modelview"), false, ref v.ModelViewProjectionMatrix);

                if (shaders[activeShader].GetAttribute("maintexture") != -1)
                {
                    GL.Uniform1(shaders[activeShader].GetAttribute("maintexture"), v.TextureID);
                }

                    if (shaders[activeShader].GetUniform("view") != -1)
                {
                    GL.UniformMatrix4(shaders[activeShader].GetUniform("view"), false, ref view);
                }

                if (shaders[activeShader].GetUniform("model") != -1)
                {
                    GL.UniformMatrix4(shaders[activeShader].GetUniform("model"), false, ref v.ModelMatrix);
                }

                if (shaders[activeShader].GetUniform("cameraPosition") != -1)
                {
                    GL.Uniform3(shaders[activeShader].GetUniform("cameraPosition"), ref cam.Position);
                }

                if (shaders[activeShader].GetUniform("farplane") != -1)
                {
                    GL.Uniform1(shaders[activeShader].GetUniform("farplane"),this.MaxRenderDistance);
                }

                if (shaders[activeShader].GetUniform("material_ambient") != -1)
                {
                    GL.Uniform3(shaders[activeShader].GetUniform("material_ambient"), ref v.Material.AmbientColor);
                }

                if (shaders[activeShader].GetUniform("material_diffuse") != -1)
                {
                    GL.Uniform3(shaders[activeShader].GetUniform("material_diffuse"), ref v.Material.DiffuseColor);
                }

                if (shaders[activeShader].GetUniform("material_specular") != -1)
                {
                    GL.Uniform3(shaders[activeShader].GetUniform("material_specular"), ref v.Material.SpecularColor);
                }

                if (shaders[activeShader].GetUniform("material_specExponent") != -1)
                {
                    GL.Uniform1(shaders[activeShader].GetUniform("material_specExponent"), v.Material.SpecularExponent);
                }

                if (shaders[activeShader].GetUniform("map_specular") != -1)
                {
                    // Object has a specular map
                    if (v.Material.SpecularMap != "")
                    {
                        GL.ActiveTexture(TextureUnit.Texture1);
                        GL.BindTexture(TextureTarget.Texture2D, textures[v.Material.SpecularMap]);
                        GL.Uniform1(shaders[activeShader].GetUniform("map_specular"), 1);
                        GL.Uniform1(shaders[activeShader].GetUniform("hasSpecularMap"), 1);
                        GL.ActiveTexture(TextureUnit.Texture0);
                    }
                    else // Object has no specular map
                    {
                        GL.Uniform1(shaders[activeShader].GetUniform("hasSpecularMap"), 0);
                    }
                }

                if (shaders[activeShader].GetUniform("light_position") != -1)
                {
                    GL.Uniform3(shaders[activeShader].GetUniform("light_position"), ref lights[0].Position);
                }

                if (shaders[activeShader].GetUniform("light_color") != -1)
                {
                    GL.Uniform3(shaders[activeShader].GetUniform("light_color"), ref lights[0].Color);
                }

                if (shaders[activeShader].GetUniform("light_diffuseIntensity") != -1)
                {
                    GL.Uniform1(shaders[activeShader].GetUniform("light_diffuseIntensity"), lights[0].DiffuseIntensity);
                }

                if (shaders[activeShader].GetUniform("light_ambientIntensity") != -1)
                {
                    GL.Uniform1(shaders[activeShader].GetUniform("light_ambientIntensity"), lights[0].AmbientIntensity);
                }


                for (int i = 0; i < Math.Min(lights.Count, MAX_LIGHTS); i++)
                {
                    if (shaders[activeShader].GetUniform("lights[" + i + "].position") != -1)
                    {
                        GL.Uniform3(shaders[activeShader].GetUniform("lights[" + i + "].position"), ref lights[i].Position);
                    }

                    if (shaders[activeShader].GetUniform("lights[" + i + "].color") != -1)
                    {
                        GL.Uniform3(shaders[activeShader].GetUniform("lights[" + i + "].color"), ref lights[i].Color);
                    }

                    if (shaders[activeShader].GetUniform("lights[" + i + "].diffuseIntensity") != -1)
                    {
                        GL.Uniform1(shaders[activeShader].GetUniform("lights[" + i + "].diffuseIntensity"), lights[i].DiffuseIntensity);
                    }

                    if (shaders[activeShader].GetUniform("lights[" + i + "].ambientIntensity") != -1)
                    {
                        GL.Uniform1(shaders[activeShader].GetUniform("lights[" + i + "].ambientIntensity"), lights[i].AmbientIntensity);
                    }

                    if (shaders[activeShader].GetUniform("lights[" + i + "].direction") != -1)
                    {
                        GL.Uniform3(shaders[activeShader].GetUniform("lights[" + i + "].direction"), ref lights[i].Direction);
                    }

                    if (shaders[activeShader].GetUniform("lights[" + i + "].type") != -1)
                    {
                        GL.Uniform1(shaders[activeShader].GetUniform("lights[" + i + "].type"), (int)lights[i].Type);
                    }

                    if (shaders[activeShader].GetUniform("lights[" + i + "].coneAngle") != -1)
                    {
                        GL.Uniform1(shaders[activeShader].GetUniform("lights[" + i + "].coneAngle"), lights[i].ConeAngle);
                    }

                    if (shaders[activeShader].GetUniform("lights[" + i + "].linearAttenuation") != -1)
                    {
                        GL.Uniform1(shaders[activeShader].GetUniform("lights[" + i + "].linearAttenuation"), lights[i].LinearAttenuation);
                    }

                    if (shaders[activeShader].GetUniform("lights[" + i + "].quadraticAttenuation") != -1)
                    {
                        GL.Uniform1(shaders[activeShader].GetUniform("lights[" + i + "].quadraticAttenuation"), lights[i].QuadraticAttenuation);
                    }
                }

                GL.DrawElements(BeginMode.Triangles, v.IndiceCount, DrawElementsType.UnsignedInt, indiceat * sizeof(uint));
                indiceat += v.IndiceCount;
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            ProcessInput();


            dtime = (float)args.Time;
            time += dtime;
            this.Title = $"LSEngine - Sponza | {(int)(1/dtime)} FPS, Current shader: {activeShader}";
            if (refreshCounter++ % maxRefresh == 0)
            {

#if PrintFPS
                Console.WriteLine(1 / (dtime) + " FPS");
#endif
                if (objectsListHasChanged)
                {
                    List<Vector3> verts = new();
                    List<int> inds = new();
                    List<Vector3> colors = new();
                    List<Vector2> texcoords = new();
                    List<Vector3> normals = new();

                    // Assemble vertex and indice data for all volumes
                    int vertcount = 0;
                    foreach (RenderObject v in objects)
                    {
                        verts.AddRange(v.GetVerts());
                        inds.AddRange(v.GetIndices(vertcount));
                        colors.AddRange(v.GetColorData());
                        texcoords.AddRange(v.GetTextureCoords());
                        normals.AddRange(v.GetNormals());
                        vertcount += v.VertCount;
                    }

                    vertdata = verts.ToArray();
                    indicedata = inds.ToArray();
                    coldata = colors.ToArray();
                    texcoorddata = texcoords.ToArray();
                    normdata = normals.ToArray();
                    objectsListHasChanged = false;
                }
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].GetBuffer("vPosition"));
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(vertdata.Length * Vector3.SizeInBytes), vertdata, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(shaders[activeShader].GetAttribute("vPosition"),3, VertexAttribPointerType.Float, false, 0, IntPtr.Zero);

            // Buffer vertex color if shader supports it
            if (shaders[activeShader].GetAttribute("vColor") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].GetBuffer("vColor"));
                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(coldata.Length * Vector3.SizeInBytes), coldata, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(shaders[activeShader].GetAttribute("vColor"), 3, VertexAttribPointerType.Float, true, 0, 0);

            }


            // Buffer texture coordinates if shader supports it
            if (shaders[activeShader].GetAttribute("texcoord") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].GetBuffer("texcoord"));
                GL.BufferData<Vector2>(BufferTarget.ArrayBuffer, (IntPtr)(texcoorddata.Length * Vector2.SizeInBytes), texcoorddata, BufferUsageHint.StaticDraw);

                GL.VertexAttribPointer(shaders[activeShader].GetAttribute("texcoord"), 2, VertexAttribPointerType.Float, true, 0, 0);

            }

            if (shaders[activeShader].GetAttribute("vNormal") != -1)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, shaders[activeShader].GetBuffer("vNormal"));
                GL.BufferData<Vector3>(BufferTarget.ArrayBuffer, (IntPtr)(normdata.Length * Vector3.SizeInBytes), normdata, BufferUsageHint.StaticDraw);
                GL.VertexAttribPointer(shaders[activeShader].GetAttribute("vNormal"), 3, VertexAttribPointerType.Float, true, 0, 0);
            }
            // Update object positions


            // Update model view matrices
            foreach (RenderObject v in objects)
            {
                v.CalculateModelMatrix();
                v.ViewProjectionMatrix = cam.GetViewMatrix() * Matrix4.CreatePerspectiveFieldOfView(1.3f, ClientSize.X / (float)ClientSize.Y, MinRenderDistance, MaxRenderDistance);
                v.ModelViewProjectionMatrix = v.ModelMatrix * v.ViewProjectionMatrix;
            }

            GL.UseProgram(shaders[activeShader].ProgramID);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // Buffer index data
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo_elements);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indicedata.Length * sizeof(int)), indicedata, BufferUsageHint.StaticDraw);

            view = cam.GetViewMatrix();
        }

        private void ProcessInput()
        {
            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            if (KeyboardState.IsKeyDown(Keys.W))
            {
                cam.Move(0f, 0.1f, 0f);

            }

            if (KeyboardState.IsKeyDown(Keys.S))
            {
                cam.Move(0f, -0.1f, 0f);

            }

                if (KeyboardState.IsKeyDown(Keys.A))
            {
                cam.Move(-0.1f, 0f, 0f);

            }

            if (KeyboardState.IsKeyDown(Keys.D))
            {
                cam.Move(0.1f, 0f, 0f);

            }

            if (KeyboardState.IsKeyDown(Keys.Space))
            {
                cam.Move(0f, 0f, 0.1f);

            }

            if (KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                cam.Move(0f, 0f, -0.1f);

            }
            if (KeyboardState.IsKeyPressed(Keys.F))
            {
                if (!IsFullscreen)
                    this.WindowState = WindowState.Fullscreen;
                else
                    this.WindowState = WindowState.Normal;
                GL.Viewport(0, 0, Size.X, Size.Y);
            }
            if (KeyboardState.IsKeyPressed(Keys.L))
                PrintLights();
            if (KeyboardState.IsKeyDown(Keys.LeftControl))
                cam.MoveSpeed = 15f;
            else
            {
                cam.MoveSpeed = SceneSettings.CameraSettings.CameraSpeed;
            }
            if (KeyboardState.IsKeyPressed(Keys.D0))
                this.lights[0].AmbientIntensity += 0.1f;
            if (KeyboardState.IsKeyPressed(Keys.D9))
                this.lights[0].AmbientIntensity -= 0.1f;

            if (KeyboardState.IsKeyPressed(Keys.P))
            {
                activeShader = shadersList[currentShaderIndex++];
                if (currentShaderIndex == shadersList.Count)
                {
                    currentShaderIndex = 0;
                }
            }
            if (IsFocused)
            {
                Vector2 delta = lastMousePos - new Vector2(MouseState.X,MouseState.Y);
                lastMousePos += delta;

                cam.AddRotation(delta.X, delta.Y);

                lastMousePos = new Vector2(MouseState.X, MouseState.Y);
            }

        }

        private void PrintLights()
        {
            for (int i = 0; i < lights.Count; i++)
            {
                Console.WriteLine( i + " " + lights[i].Type +  " Brightness: " + lights[i].AmbientIntensity);
            }
        }

        protected override void OnFocusedChanged(FocusedChangedEventArgs e)
        {
            base.OnFocusedChanged(e);
            lastMousePos = new Vector2(MouseState.X, MouseState.Y);
        }

        internal void PrintControls()
        {
            Console.WriteLine("Press Escape to close the window;");
            Console.WriteLine("Press W, S, A, D, Space, LeftShift to move;");
            Console.WriteLine("Press F for fulscreen mode;");
            Console.WriteLine("Press L to print lights brightness;");
            Console.WriteLine("Press 9 or 0 to increase/decrease the brightness;");
            Console.WriteLine("Press P to switch between shaders;");
        }


    }
}