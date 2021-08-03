using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSEngine.Common
{
    class FaceVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoord;

        public FaceVertex(Vector3 pos, Vector3 norm, Vector2 texcoord)
        {
            Position = pos;
            Normal = norm;
            TextureCoord = texcoord;
        }
    }

    class ObjRenderObject : RenderObject
    {
        private List<Tuple<FaceVertex, FaceVertex, FaceVertex>> faces = new();

        public override int VertCount { get { return faces.Count * 3; } }

        public override int IndiceCount { get { return faces.Count * 3; } }

        public override int ColorDataCount { get { return faces.Count * 3; } }

        public override int TextureCoordsCount { get { return faces.Count * 3; } }


        public override Vector3[] GetNormals()
        {
            if (base.GetNormals().Length > 0)
            {
                return base.GetNormals();
            }

            List<Vector3> normals = new();

            foreach (var face in faces)
            {
                normals.Add(face.Item1.Normal);
                normals.Add(face.Item2.Normal);
                normals.Add(face.Item3.Normal);
            }

            return normals.ToArray();
        }

        public override int NormalCount
        {
            get
            {
                return faces.Count * 3;
            }
        }

        public List<ObjRenderObject> Parts { get; set; } = new();

        
        public override Vector3[] GetVerts()
        {
            List<Vector3> verts = new();

            foreach (var face in faces)
            {
                verts.Add(face.Item1.Position);
                verts.Add(face.Item2.Position);
                verts.Add(face.Item3.Position);
            }

            return verts.ToArray();
        }

       
        public override int[] GetIndices(int offset = 0)
        {
            return Enumerable.Range(offset, IndiceCount).ToArray();
        }

      
        public override Vector3[] GetColorData()
        {
            return new Vector3[ColorDataCount];
        }

       
        public override Vector2[] GetTextureCoords()
        {
            List<Vector2> coords = new();

            foreach (var face in faces)
            {
                coords.Add(face.Item1.TextureCoord);
                coords.Add(face.Item2.TextureCoord);
                coords.Add(face.Item3.TextureCoord);
            }

            return coords.ToArray();
        }

        
        public override void CalculateModelMatrix()
        {
            ModelMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        }

        
        public static ObjRenderObject LoadFromFile(string filename, Dictionary<string, Material> materials, Dictionary<string, int> textures)
        {
            ObjRenderObject obj = new();
            try
            {
                using (StreamReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read)))
                {
                    obj = LoadFromString(reader.ReadToEnd(), materials, textures);
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("File not found: {0}", filename);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error loading file: {0}.\n{1}", filename, e);
            }

            return obj;
        }

        public static ObjRenderObject LoadFromString(string obj, Dictionary<string, Material> materials, Dictionary<string, int> textures)
        {
            ObjRenderObject newObject = new();
            int partsIndex = -1;
            // Seperate lines from the file
            List<String> lines = new(obj.Split('\n'));

            // Lists to hold model data
            List<Vector3> verts = new();
            List<Vector3> normals = new();
            List<Vector2> texs = new();
            List<Tuple<TempVertex, TempVertex, TempVertex>> faces = new();

            // Base values
            verts.Add(new Vector3());
            texs.Add(new Vector2());
            normals.Add(new Vector3());

            int currentindice = 0;

            // Read file line by line
            foreach (String line in lines)
            {
                if (line.StartsWith("g ")) // new group definition
                {
                    if (newObject.Parts.Count != 0)
                    {
                        foreach (var face in faces)
                        {
                            FaceVertex v1 = new(verts[face.Item1.Vertex], normals[face.Item1.Normal], texs[face.Item1.Texcoord]);
                            FaceVertex v2 = new(verts[face.Item2.Vertex], normals[face.Item2.Normal], texs[face.Item2.Texcoord]);
                            FaceVertex v3 = new(verts[face.Item3.Vertex], normals[face.Item3.Normal], texs[face.Item3.Texcoord]);

                            newObject.Parts[partsIndex].faces.Add(new Tuple<FaceVertex, FaceVertex, FaceVertex>(v1, v2, v3));
                        }
                    }
                    newObject.Parts.Add(new());
                    partsIndex++;
                    faces = new();
                }
                if (line.StartsWith("usemtl ")) //use material
                {
                    newObject.Parts[partsIndex].Material = materials[line.Substring(6).Trim()];
                    try
                    {
                        newObject.Parts[partsIndex].TextureID = textures[materials[line.Substring(6).Trim()].DiffuseMap];
                    }
                    catch { }
                }
                if (line.StartsWith("v ")) // Vertex definition
                {
                    string temp = line.Substring(2);

                    Vector3 vec = new();

                    if (temp.Trim().Count((char c) => c == ' ') == 2) // Check if there's enough elements for a vertex
                    {
                        string[] vertparts = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        bool success = float.TryParse(vertparts[0], out vec.X);
                        success |= float.TryParse(vertparts[1], out vec.Y);
                        success |= float.TryParse(vertparts[2], out vec.Z);

                        if (!success)
                        {
                            Console.WriteLine("Error parsing vertex: {0}", line);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error parsing vertex: {0}", line);
                    }

                    verts.Add(vec);
                }
                else if (line.StartsWith("vt ")) // Texture coordinate
                {
                    string temp = line.Substring(2);

                    Vector2 vec = new();

                    if (temp.Trim().Any((char c) => c == ' ')) // Check if there's enough elements for a vertex
                    {
                        string[] texcoordparts = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        bool success = float.TryParse(texcoordparts[0], out vec.X);
                        success |= float.TryParse(texcoordparts[1], out vec.Y);

                        if (!success)
                        {
                            Console.WriteLine("Error parsing texture coordinate: {0}", line);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error parsing texture coordinate: {0}", line);
                    }

                    texs.Add(vec);
                }
                else if (line.StartsWith("vn ")) // Normal vector
                {
                    string temp = line.Substring(2);

                    Vector3 vec = new();

                    if (temp.Trim().Count((char c) => c == ' ') == 2) // Check if there's enough elements for a normal
                    {
                        string[] vertparts = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        // Attempt to parse each part of the vertice
                        bool success = float.TryParse(vertparts[0], out vec.X);
                        success |= float.TryParse(vertparts[1], out vec.Y);
                        success |= float.TryParse(vertparts[2], out vec.Z);

                        if (!success)
                        {
                            Console.WriteLine("Error parsing normal: {0}", line);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error parsing normal: {0}", line);
                    }

                    normals.Add(vec);
                }
                else if (line.StartsWith("f ")) // Face definition
                {
                  string temp = line.Substring(2);


                    Tuple<TempVertex, TempVertex, TempVertex> face = new(new TempVertex(), new TempVertex(), new TempVertex());

                    if (temp.Trim().Count((char c) => c == ' ') == 2) // Check if there's enough elements for a face
                    {
                        string[] faceparts = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        int t1, t2, t3;
                        int n1, n2, n3;

                        bool success = int.TryParse(faceparts[0].Split('/')[0], out int v1);
                        success |= int.TryParse(faceparts[1].Split('/')[0], out int v2);
                        success |= int.TryParse(faceparts[2].Split('/')[0], out int v3);

                        if (faceparts[0].Count((char c) => c == '/') >= 2)
                        {
                            success |= int.TryParse(faceparts[0].Split('/')[1], out t1);
                            success |= int.TryParse(faceparts[1].Split('/')[1], out t2);
                            success |= int.TryParse(faceparts[2].Split('/')[1], out t3);
                            success |= int.TryParse(faceparts[0].Split('/')[2], out n1);
                            success |= int.TryParse(faceparts[1].Split('/')[2], out n2);
                            success |= int.TryParse(faceparts[2].Split('/')[2], out n3);
                        }
                        else
                        {
                            if (texs.Count > v1 && texs.Count > v2 && texs.Count > v3)
                            {
                                t1 = v1;
                                t2 = v2;
                                t3 = v3;
                            }
                            else
                            {
                                t1 = 0;
                                t2 = 0;
                                t3 = 0;
                            }


                            if (normals.Count > v1 && normals.Count > v2 && normals.Count > v3)
                            {
                                n1 = v1;
                                n2 = v2;
                                n3 = v3;
                            }
                            else
                            {
                                n1 = 0;
                                n2 = 0;
                                n3 = 0;
                            }
                        }


                        if (!success)
                        {
                            Console.WriteLine("Error parsing face: {0}", line);
                        }
                        else
                        {
                            TempVertex tv1 = new(v1, n1, t1);
                            TempVertex tv2 = new(v2, n2, t2);
                            TempVertex tv3 = new(v3, n3, t3);
                            face = new Tuple<TempVertex, TempVertex, TempVertex>(tv1, tv2, tv3);
                            faces.Add(face);
                        }
                    }
                    else if (temp.Trim().Count((char c) => c == ' ') == 3)
                    {

                        String[] faceparts = temp.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        Tuple<TempVertex, TempVertex, TempVertex> facetwo = new(new(),new(),new());

                        int t1, t2, t3, t4;
                        int n1, n2, n3, n4;

                        bool success = int.TryParse(faceparts[0].Split('/')[0], out int v1);
                        success |= int.TryParse(faceparts[1].Split('/')[0], out int v2);
                        success |= int.TryParse(faceparts[2].Split('/')[0], out int v3);
                        success |= int.TryParse(faceparts[3].Split('/')[0], out int v4);

                        if (faceparts[0].Count((char c) => c == '/') >= 2)
                        {
                            success |= int.TryParse(faceparts[0].Split('/')[1], out t1);
                            success |= int.TryParse(faceparts[1].Split('/')[1], out t2);
                            success |= int.TryParse(faceparts[2].Split('/')[1], out t3);
                            success |= int.TryParse(faceparts[3].Split('/')[1], out t4);
                            success |= int.TryParse(faceparts[0].Split('/')[2], out n1);
                            success |= int.TryParse(faceparts[1].Split('/')[2], out n2);
                            success |= int.TryParse(faceparts[2].Split('/')[2], out n3);
                            success |= int.TryParse(faceparts[3].Split('/')[2], out n4);
                        }
                        else
                        {
                            if (texs.Count > v1 && texs.Count > v2 && texs.Count > v3 && texs.Count > v4)
                            {
                                t1 = v1;
                                t2 = v2;
                                t3 = v3;
                                t4 = v4;
                            }
                            else
                            {
                                t1 = 0;
                                t2 = 0;
                                t3 = 0;
                                t4 = 0;
                            }


                            if (normals.Count > v1 && normals.Count > v2 && normals.Count > v3 && normals.Count > v4)
                            {
                                n1 = v1;
                                n2 = v2;
                                n3 = v3;
                                n4 = v4;
                            }
                            else
                            {
                                n1 = 0;
                                n2 = 0;
                                n3 = 0;
                                n4 = 0;
                            }
                        }


                        if (!success)
                        {
                            Console.WriteLine("Error parsing face: {0}", line);
                        }
                        else
                        {
                            TempVertex tv1 = new(v1, n1, t1);
                            TempVertex tv2 = new(v2, n2, t2);
                            TempVertex tv3 = new(v3, n3, t3);
                            TempVertex tv4 = new(v4, n4, t4);
                            face = new(tv1, tv2, tv3);
                            facetwo = new(tv1, tv3, tv4);
                            faces.Add(face);
                            faces.Add(facetwo);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error parsing face: {0}", line);
                    }
                }
            }

            return newObject;
        }

        public override bool InView(Vector3 camLookAt, float fov)
        {
                Vector3[] vertices = GetVerts();
                foreach (var vertex in vertices)
                {
                    if(fov+0.2 < Vector3.CalculateAngle(vertex, camLookAt))
                    {
                        return false;
                    }
                }
            return true;
        }

        private class TempVertex
        {
            public int Vertex;
            public int Normal;
            public int Texcoord;

            public TempVertex(int vert = 0, int norm = 0, int tex = 0)
            {
                Vertex = vert;
                Normal = norm;
                Texcoord = tex;
            }
        }
    }
}
