using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LSEngine.Common
{
    class ShaderProgram
    {
        public int ProgramID = -0;
        public int VShaderID = 0;
        public int FShaderID = 0;
        public int AttributeCount = 0;
        public int UniformCount = 0;

        public Dictionary<String, AttributeInfo> Attributes = new();
        public Dictionary<String, UniformInfo> Uniforms = new();
        public Dictionary<String, uint> Buffers = new();


        public ShaderProgram(string vs, string fs, bool fromFile = false)
        {
            Console.WriteLine($"creating {vs} / {fs} shader");

            ProgramID = GL.CreateProgram();

            if (fromFile)
            {
                LoadShaderFromFile(vs, ShaderType.VertexShader);
                LoadShaderFromFile(fs, ShaderType.FragmentShader);

            }
            else
            {
                LoadShaderFromString(vs, ShaderType.VertexShader);
                LoadShaderFromString(fs, ShaderType.FragmentShader);
            }

            LinkProgram();
            GenerateBuffers();
        }

        private void LoadShaderFromString(string code, ShaderType type)
        {
            if (type == ShaderType.VertexShader)
            {
                LoadShader(code, type, out VShaderID);
            }
            else if (type == ShaderType.FragmentShader)
            {
                LoadShader(code, type, out FShaderID);
            }
        }

        private void LoadShader(string code, ShaderType type, out int address)
        {
            address = GL.CreateShader(type);
            GL.ShaderSource(address, code);
            GL.CompileShader(address);
            GL.AttachShader(ProgramID, address);
            string log = GL.GetShaderInfoLog(address);
            if (log != "") Console.WriteLine(log);
        }

        private void LoadShaderFromFile(string filename , ShaderType type)
        {
            using (StreamReader sr = new(filename))
            {
                if (type == ShaderType.VertexShader)
                {
                    LoadShader(sr.ReadToEnd(), type, out VShaderID);
                }
                else if (type == ShaderType.FragmentShader)
                {
                    LoadShader(sr.ReadToEnd(), type, out FShaderID);
                }
            }
        }

        private void GenerateBuffers()
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.GenBuffers(1, out uint buffer);

                Buffers.Add(Attributes.Values.ElementAt(i).name, buffer);
            }

            for (int i = 0; i < Uniforms.Count; i++)
            {
                GL.GenBuffers(1, out uint buffer);

                Buffers.Add(Uniforms.Values.ElementAt(i).name, buffer);
            }
        }
        public void EnableVertexAttribArrays()
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.EnableVertexAttribArray(Attributes.Values.ElementAt(i).address);
            }
        }

        public void DisableVertexAttribArrays()
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                GL.DisableVertexAttribArray(Attributes.Values.ElementAt(i).address);
            }
        }

        private void LinkProgram()
        {
            GL.LinkProgram(ProgramID);

            Console.WriteLine(GL.GetProgramInfoLog(ProgramID));

            GL.GetProgram(ProgramID, GetProgramParameterName.ActiveAttributes, out AttributeCount);
            GL.GetProgram(ProgramID, GetProgramParameterName.ActiveUniforms, out UniformCount);

            for (int i = 0; i < AttributeCount; i++)
            {
                AttributeInfo info = new();
                GL.GetActiveAttrib(ProgramID, i, 256, out _, out info.size, out info.type, out string name);

                info.name = name.ToString();
                info.address = GL.GetAttribLocation(ProgramID, info.name);
                Attributes.Add(name.ToString(), info);
            }

            for (int i = 0; i < UniformCount; i++)
            {
                UniformInfo info = new();
                GL.GetActiveUniform(ProgramID, i, 256, out _, out info.size, out info.type, out string name);

                info.name = name.ToString();
                Uniforms.Add(name.ToString(), info);
                info.address = GL.GetUniformLocation(ProgramID, info.name);
            }
        }

        public int GetAttribute(string name)
        {
            if (Attributes.ContainsKey(name))
            {
                return Attributes[name].address;
            }
            else
            {
                return -1;
            }
        }

        public int GetUniform(string name)
        {
            if (Uniforms.ContainsKey(name))
            {
                return Uniforms[name].address;
            }
            else
            {
                return -1;
            }
        }

        public uint GetBuffer(string name)
        {
            if (Buffers.ContainsKey(name))
            {
                return Buffers[name];
            }
            else
            {
                return 0;
            }
        }
    }

    public class UniformInfo
    {
        public String name = "";
        public int address = 0;
        public int size = 0;
        public ActiveUniformType type;
    }

    public class AttributeInfo
    {
        public String name = "";
        public int address = 0;
        public int size = 0;
        public ActiveAttribType type;
    }
}
