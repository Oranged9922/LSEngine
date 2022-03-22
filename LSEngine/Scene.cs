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

        //int frameBufferName;
        //int renderedTexture;
        //int depthRenderBuffer;


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
        string activeShader;
        int currentShaderIndex = 0;
        Dictionary<String, Material> materials = new();
        Dictionary<string, Action<ShaderProgram, RenderObject>> UniformsAndAttribsDelegates = new();
        float time = 0.0f;
        float dtime = 0.0f;
        int refreshCounter = 0;
        float fov = 1.3f;
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
            Console.WriteLine($"Graphics card used: {GL.GetString(StringName.Vendor)}, GL version: {GL.GetString(StringName.Version)}");
        }
        private void InitializeProgram()
        {
            lastMousePos = new Vector2(MouseState.X, MouseState.Y);
            CursorVisible = false;
            CursorGrabbed = true;
            cam.MouseSensitivity = 0.025f;
            cam.MoveSpeed = 2;
            ibo_elements = GL.GenBuffer();
            //frameBufferName = GL.GenBuffer();
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferName);
            //renderedTexture = GL.GenTexture();
            //GL.BindTexture(TextureTarget.Texture2D, renderedTexture);
            //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, ClientSize.X, ClientSize.Y, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);

            //depthRenderBuffer = GL.GenRenderbuffer();
            //GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRenderBuffer);
            //GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, ClientSize.X, ClientSize.Y);
            //GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthRenderBuffer);
            //GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, renderedTexture, 0);

            //GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            //if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete) Console.WriteLine("OOh");

            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            LoadResources();

            activeShader = shadersList[0];
            
            cam.Position += new Vector3(0.0f, 5.0f, 0.0f);
            SetupScene();
            CreateDelegates();
        }
        private void CreateDelegates()
        {
            UniformsAndAttribsDelegates.Add(
                "modelview",
                (s, o) =>
                    {
                        GL.UniformMatrix4(s.GetUniform("modelview"), false, ref o.ModelViewProjectionMatrix);
                    }); 

            UniformsAndAttribsDelegates.Add(
                "lightSpaceMatrix",
                (s, o) =>
                {
                    GetLightSpaceMatrix(lights[0]);
                    lights[0].ModelViewProjectionMatrix = o.ModelMatrix * lights[0].ViewProjectionMatrix;
                    GL.UniformMatrix4(s.GetUniform("lightSpaceMatrix"), false, ref lights[0].ModelViewProjectionMatrix);
                });

            UniformsAndAttribsDelegates.Add(
                "viewprojectionLight",
                (s, o) =>
                {
                    GetLightSpaceMatrix(lights[0]);
                    lights[0].ModelViewProjectionMatrix = o.ModelMatrix * lights[0].ViewProjectionMatrix;

                    GL.UniformMatrix4(s.GetUniform("viewprojectionLight"), false, ref lights[0].ViewProjectionMatrix);
                });

            UniformsAndAttribsDelegates.Add(
                "maintexture",
                (s, o) =>
                {
                    GL.Uniform1(s.GetAttribute("maintexture"), o.TextureID);
                });

            UniformsAndAttribsDelegates.Add(
                "view",
                (s, o) =>
                {
                    GL.UniformMatrix4(s.GetUniform("view"), false, ref view);

                });

            UniformsAndAttribsDelegates.Add(
                "model",
                (s, o) =>
                {
                    GL.UniformMatrix4(s.GetUniform("model"), false, ref o.ModelMatrix);
                });

            UniformsAndAttribsDelegates.Add(
                "cameraPosition",
                (s, o) =>
                {
                    GL.Uniform3(s.GetUniform("cameraPosition"), ref cam.Position);
                });

            UniformsAndAttribsDelegates.Add(
                "farplane",
                (s, o) =>
                {
                    GL.Uniform1(s.GetUniform("farplane"), this.MaxRenderDistance);
                });

            UniformsAndAttribsDelegates.Add(
                "material_ambient",
                (s, o) =>
                {
                    GL.Uniform3(s.GetUniform("material_ambient"), ref o.Material.AmbientColor);
                });

            UniformsAndAttribsDelegates.Add(
                "material_specular",
                (s, o) =>
                {
                    GL.Uniform3(s.GetUniform("material_specular"), ref o.Material.SpecularColor);
                });

            UniformsAndAttribsDelegates.Add(
                "material_diffuse",
                (s, o) =>
                {
                    GL.Uniform3(s.GetUniform("material_diffuse"), ref o.Material.DiffuseColor);
                });

            UniformsAndAttribsDelegates.Add(
                "material_specExponent",
                (s, o) =>
                {
                    GL.Uniform1(s.GetUniform("material_specExponent"), o.Material.SpecularExponent);
                });

            UniformsAndAttribsDelegates.Add(
                "diffuseTexture",
                (s, o) =>
                {
                    GL.Uniform3(s.GetUniform("diffuseTexture"), ref o.Material.DiffuseColor);
                });

            //UniformsAndAttribsDelegates.Add(
            //    "shadowMap",
            //    (s, o) =>
            //    {
            //        if (o.Material.SpecularMap != "")
            //        {
            //            GL.ActiveTexture(TextureUnit.Texture1);
            //            GL.BindTexture(TextureTarget.Texture2D, renderedTexture);
            //            GL.Uniform1(s.GetUniform("shadowMap"), 1);
            //            GL.ActiveTexture(TextureUnit.Texture0);
            //        }
            //        else // Object has no specular map
            //        {
            //            GL.Uniform1(s.GetUniform("hasSpecularMap"), 0);
            //        }
            //    });

            UniformsAndAttribsDelegates.Add(
                "map_specular",
                (s, o) =>
                {
                    // Object has a specular map
                    if (o.Material.SpecularMap != "")
                    {
                        GL.ActiveTexture(TextureUnit.Texture1);
                        GL.BindTexture(TextureTarget.Texture2D, textures[o.Material.SpecularMap]);
                        GL.Uniform1(s.GetUniform("map_specular"), 1);
                        GL.Uniform1(s.GetUniform("hasSpecularMap"), 1);
                        GL.ActiveTexture(TextureUnit.Texture0);
                    }
                    else // Object has no specular map
                    {
                        GL.Uniform1(s.GetUniform("hasSpecularMap"), 0);
                    }
                });

            UniformsAndAttribsDelegates.Add(
                "viewPos",
                (s, o) =>
                {
                    GL.Uniform3(s.GetUniform("viewPos"), ref cam.Position);
                });

            UniformsAndAttribsDelegates.Add(
               "light_position",
               (s, o) =>
               {
                   GL.Uniform3(s.GetUniform("light_position"), ref lights[0].Position);
               });

            UniformsAndAttribsDelegates.Add(
               "lightPos",
               (s, o) =>
               {
                   GL.Uniform3(s.GetUniform("lightPos"), ref lights[0].Position);
               });

            UniformsAndAttribsDelegates.Add(
               "light_color",
               (s, o) =>
               {
                   GL.Uniform3(shaders[activeShader].GetUniform("light_color"), ref lights[0].Color);
               });

            UniformsAndAttribsDelegates.Add(
               "light_diffuseIntensity",
               (s, o) =>
               {
                   GL.Uniform1(shaders[activeShader].GetUniform("light_diffuseIntensity"), lights[0].DiffuseIntensity);
               });
            UniformsAndAttribsDelegates.Add(
               "light_ambientIntensity",
               (s, o) =>
               {
                   GL.Uniform1(shaders[activeShader].GetUniform("light_ambientIntensity"), lights[0].AmbientIntensity);
               });
        }
        private void LoadResources()
        {
            // Load Materials and Textures
            LoadMaterials("Materials/sponza.mtl");
            LoadMaterials("Materials/cube.mtl");
            LoadShaders(shaders, shadersList, "Shaders");


        }
        private void LoadShaders(Dictionary<string, ShaderProgram> shaders, List<string> shadersList, string path)
        {
            var files = new DirectoryInfo(path).GetFiles();
            var tuples = new Dictionary<string, (string vs, string fs)>();
            foreach (var file in files)
            {
                var tempName = file.Name[3..^5];
                var pathToFile = path + "/" + file.Name;
                if (file.Name.StartsWith("vs"))
                {
                    
                    if (!tuples.ContainsKey(tempName))
                    {
                        tuples.Add(tempName, new(pathToFile, default));
                    }
                    else
                    {
                        var t = tuples[tempName];
                        t.vs = (t.vs == default) ? pathToFile: throw new Exception($"two shaders with same name! {file.Name}");
                        tuples[tempName] = t;
                    }
                }
                else if (file.Name.StartsWith("fs"))
                {
                    if (!tuples.ContainsKey(tempName))
                    {
                        tuples.Add(tempName, new(default, pathToFile));
                    }
                    else
                    {
                        var t = tuples[tempName];
                        t.fs = (t.fs == default) ? pathToFile : throw new Exception($"two shaders with same name! {file.Name}");
                        tuples[tempName] = t;
                    }
                }
                else throw new Exception($"Unexpected filename, does not start with \"vs\" or \"fs\" :{pathToFile}");
            }

            foreach (var shad in tuples)
            {
                shaders.Add(shad.Key, new(shad.Value.vs, shad.Value.fs, true));
                shadersList.Add(shad.Key);
            }
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
                var maps = mat.GetMaps();
                foreach (var map in maps)
                {
                    if (File.Exists(map) && !textures.ContainsKey(map))
                    {
                        textures.Add(map, LoadImage(map));
                    }
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
        static int LoadImage(Bitmap image)
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
            foreach (var part in sponza.Parts) objects.Add(part);

            ObjRenderObject cube = ObjRenderObject.LoadFromFile("ObjectFiles/cube.obj", materials, textures);
            cube.Scale = new(5, 5, 5);

            foreach (var part in cube.Parts) objects.Add(part);

            //objects.Add(RenderObject.PrimitiveObjects.Cube);

            Light sunLight = new(new Vector3(-100, 2000, -100), new Vector3(1), 0.9f, 0.9f);
            sunLight.Type = LightType.Point;
            sunLight.ConeAngle = 15f;
            sunLight.Direction = new Vector3(1,-1.75f,-1).Normalized();
            lights.Add(sunLight);

            //Light light = new(new(0,2,2), new(0.6f),0.2f);
            //light.Type = LightType.Spot;
            //light.Direction = new Vector3(0, 0, 1).Normalized();
            //lights.Add(light);


            //Console.WriteLine(cam.Position);
            //Console.WriteLine(cam.Orientation);
            //Console.WriteLine("lightSource");
            //Console.WriteLine(sunLight.Position);
            //Console.WriteLine(sunLight.Direction);
        }
        void RenderScene(string activeShader)
        {
            if (!shaders.ContainsKey(activeShader)) activeShader = shadersList[0];

            var shdr = shaders[activeShader];
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(shdr.ProgramID);

            shdr.EnableVertexAttribArrays();

            int indiceat = 0;

            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            // Draw all objects
            foreach (RenderObject v in objects)
            {
                //if (!v.InView(cam.LookAt, 0.5f))
                //{
                //    indiceat += v.IndiceCount;
                //    continue;
                //}
                
                GL.BindTexture(TextureTarget.Texture2D, v.TextureID);

                // foreach uniform
                foreach (var uni in shdr.Uniforms.Keys)
                {
                    if(UniformsAndAttribsDelegates.TryGetValue(uni, out var action))
                        action.Invoke(shdr, v);
                }
                foreach (var attrib in shdr.Attributes.Keys)
                {
                    if (UniformsAndAttribsDelegates.TryGetValue(attrib, out var action))
                        action.Invoke(shdr, v);
                }


                // Lights separately
                
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
            shaders[activeShader].DisableVertexAttribArrays();
        }

        private Matrix4 GetLightSpaceMatrix(Light light)
        {
            light.ViewProjectionMatrix = 
                light.GetViewMatrix() * 
                Matrix4.CreatePerspectiveFieldOfView(fov, ClientSize.X / (float)ClientSize.Y, MinRenderDistance, MaxRenderDistance);
            return light.ViewProjectionMatrix;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            //GL.Viewport(0, 0, Size.X, Size.Y);

            //Console.WriteLine("Rendering using simpleDepthShader");
            //save this as shadow texture
            //RenderScene("simpleDepthShader");

            //Console.WriteLine("Rendering using activeShader");
            // use shadow texture and render normally;
            RenderScene(activeShader);
            GL.Disable(EnableCap.Blend);
            GL.Flush();
            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            ProcessInput();


            dtime = (float)args.Time;
            time += dtime;
            this.Title = $"LSEngine - Sponza | {(int)(1/dtime)} FPS, Current shader: {activeShader}";


            //light moving
            //lights[0].Direction += new Vector3(0, 0, 0);

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
                v.ViewProjectionMatrix = cam.GetViewMatrix() * Matrix4.CreatePerspectiveFieldOfView(fov, ClientSize.X / (float)ClientSize.Y, MinRenderDistance, MaxRenderDistance);
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

            //light move
            if (KeyboardState.IsKeyDown(Keys.U))
            {
                lights[0].Direction += new Vector3(0f, 0.1f,0f);

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
            Console.WriteLine("Press U to change light direction;");
        }


    }
}