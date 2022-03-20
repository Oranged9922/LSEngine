using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSEngine.Common
{
    public class Material
    {
        public Vector3 AmbientColor = new();
        public Vector3 DiffuseColor = new();
        public Vector3 SpecularColor = new();
        public float SpecularExponent = 1;
        public float Opacity = 1.0f;

        public string AmbientMap = "";
        public string DiffuseMap = "";
        public string SpecularMap = "";
        public string OpacityMap = "";
        public string NormalMap = "";

        public Material()
        {
        }

        public Material(Vector3 ambient, Vector3 diffuse, Vector3 specular, float specexponent = 1.0f, float opacity = 1.0f)
        {
            AmbientColor = ambient;
            DiffuseColor = diffuse;
            SpecularColor = specular;
            SpecularExponent = specexponent;
            Opacity = opacity;
        }

        public static Dictionary<string, Material> LoadFromFile(string filename)
        {
            Dictionary<string, Material> mats = new();

            try
            {
                string currentmat = "";
                using (StreamReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                {
                    string currentLine;

                    while (!reader.EndOfStream)
                    {
                        currentLine = reader.ReadLine();

                        if (!currentLine.StartsWith("newmtl"))
                        {
                            if (currentmat.StartsWith("newmtl"))
                            {
                                currentmat += currentLine + "\n";
                            }
                        }
                        else
                        {
                            if (currentmat.Length > 0)
                            {
                                Material newMat = new();

                                newMat = LoadFromString(currentmat, out string newMatName);

                                mats.Add(newMatName, newMat);
                            }

                            currentmat = currentLine + "\n";
                        }
                    }
                }

                // Add final material
                if (currentmat.Count((char c) => c == '\n') > 0)
                {
                    Material newMat = new();

                    newMat = LoadFromString(currentmat, out string newMatName);

                    mats.Add(newMatName, newMat);
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File not found: {0}", filename);
            }
            catch (Exception)
            {
                Console.WriteLine("Error loading file: {0}", filename);
            }

            return mats;
        }

        public static Material LoadFromString(string mat, out string name)
        {
            Material output = new();
            name = "";

            List<string> lines = mat.Split('\n').ToList();

            
            lines = lines.SkipWhile(s => !s.StartsWith("newmtl ")).ToList();

            
            if (lines.Count != 0)
            {
               
                name = lines[0].Substring("newmtl ".Length);
            }

            
            lines = lines.Select((string s) => s.Trim()).ToList();

           
            foreach (string line in lines)
            {
                
                if (line.Length < 3 || line.StartsWith("//") || line.StartsWith("#"))
                {
                    continue;
                }

               
                if (line.StartsWith("Ka"))
                {
                    string[] colorparts = line.Substring(3).Split(' ');

                    
                    if (colorparts.Length < 3)
                    {
                        throw new ArgumentException("Invalid color data");
                    }

                    Vector3 vec = new();

                    bool success = float.TryParse(colorparts[0], out vec.X);
                    success |= float.TryParse(colorparts[1], out vec.Y);
                    success |= float.TryParse(colorparts[2], out vec.Z);

                    output.AmbientColor = new Vector3(float.Parse(colorparts[0]), float.Parse(colorparts[1]), float.Parse(colorparts[2]));

                    if (!success)
                    {
                        Console.WriteLine("Error parsing color: {0}", line);
                    }
                }

                if (line.StartsWith("Kd"))
                {
                    string[] colorparts = line.Substring(3).Split(' ');

                    if (colorparts.Length < 3)
                    {
                        throw new ArgumentException("Invalid color data");
                    }

                    Vector3 vec = new();

                    bool success = float.TryParse(colorparts[0], out vec.X);
                    success |= float.TryParse(colorparts[1], out vec.Y);
                    success |= float.TryParse(colorparts[2], out vec.Z);

                    output.DiffuseColor = new Vector3(float.Parse(colorparts[0]), float.Parse(colorparts[1]), float.Parse(colorparts[2]));

                    if (!success)
                    {
                        Console.WriteLine("Error parsing color: {0}", line);
                    }
                }

                if (line.StartsWith("Ks"))
                {
                    String[] colorparts = line.Substring(3).Split(' ');

                    if (colorparts.Length < 3)
                    {
                        throw new ArgumentException("Invalid color data");
                    }

                    Vector3 vec = new();

                    bool success = float.TryParse(colorparts[0], out vec.X);
                    success |= float.TryParse(colorparts[1], out vec.Y);
                    success |= float.TryParse(colorparts[2], out vec.Z);

                    output.SpecularColor = new Vector3(float.Parse(colorparts[0]), float.Parse(colorparts[1]), float.Parse(colorparts[2]));

                    if (!success)
                    {
                        Console.WriteLine("Error parsing color: {0}", line);
                    }
                }

                if (line.StartsWith("Ns"))
                {
                    bool success = float.TryParse(line.Substring(3), out float exponent);

                    output.SpecularExponent = exponent;

                    if (!success)
                    {
                        Console.WriteLine("Error parsing specular exponent: {0}", line);
                    }
                }

                if (line.StartsWith("map_Ka"))
                {
                    if (line.Length > "map_Ka".Length + 6)
                    {
                        output.AmbientMap = line.Substring("map_Ka".Length + 1);
                    }
                }

                if (line.StartsWith("map_Kd"))
                {
                    if (line.Length > "map_Kd".Length + 6)
                    {
                        output.DiffuseMap = line.Substring("map_Kd".Length + 1);
                    }
                }

                if (line.StartsWith("map_Ks"))
                {
                    if (line.Length > "map_Ks".Length + 6)
                    {
                        output.SpecularMap = line.Substring("map_Ks".Length + 1);
                    }
                }

                if (line.StartsWith("map_normal"))
                {
                    if (line.Length > "map_normal".Length + 6)
                    {
                        output.NormalMap = line.Substring("map_normal".Length + 1);
                    }
                }

                if (line.StartsWith("map_opacity"))
                {
                    if (line.Length > "map_opacity".Length + 6)
                    {
                        output.OpacityMap = line.Substring("map_opacity".Length + 1);
                    }
                }

            }

            return output;
        }

        internal string[] GetMaps() => new string[] 
        {
            this.OpacityMap,
            this.AmbientMap,
            this.SpecularMap,
            this.DiffuseMap,
            this.NormalMap 
        };
    }
}
