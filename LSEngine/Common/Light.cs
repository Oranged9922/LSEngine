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

       
        public LightType Type;

        
        public Vector3 Direction;

       
        public float ConeAngle;

       
        public float LinearAttenuation;

        
        public float QuadraticAttenuation;
    }

    
    enum LightType { Point, Spot, Directional }
}

