using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSEngine.Common
{
    class Light
    {
        public Light(Vector3 position, Vector3 color, float diffuseintensity = 1.0f, float ambientintensity = 1.0f)
        {
            Position = position;
            Color = color;

            DiffuseIntensity = diffuseintensity;
            AmbientIntensity = ambientintensity;

            Type = LightType.Point;
            Direction = new Vector3(0, 0, 1);
            ConeAngle = 15.0f;
        }

        
        public Vector3 Position;

       
        public Vector3 Color;

       
        public float DiffuseIntensity;

       
        public float AmbientIntensity;

        public Vector3 LookAt;

        public Matrix4 ViewProjectionMatrix;
       
        public LightType Type;

        
        public Vector3 Direction;

       
        public float ConeAngle;

       
        public float LinearAttenuation;

        
        public float QuadraticAttenuation;

        public Matrix4 ModelViewProjectionMatrix;

        internal Matrix4 GetViewMatrix()
        {
            Vector3 lookat = new();

            lookat.X = (float)(Math.Sin((float)Direction.X) * Math.Cos((float)Direction.Y));
            lookat.Y = (float)Math.Sin((float)Direction.Y);
            lookat.Z = (float)(Math.Cos((float)Direction.X) * Math.Cos((float)Direction.Y));
            LookAt = lookat;
            return Matrix4.LookAt(Position, Position + lookat, Vector3.UnitY);
        }
    }

    
    enum LightType { Point, Spot, Directional }
}

