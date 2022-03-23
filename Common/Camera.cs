using OpenTK.Mathematics;
using System;

namespace Common
{
    public class Camera
    {
        public Vector3 Position { get; private set; }
        public Vector3 Orientation { get; private set; }
        public float MoveSpeed { get; }
        public float MouseSensitivity { get; }
        public Vector3 LookAt { get; private set; }


        public Camera()
        {
            this.Position = Vector3.Zero;
            this.Orientation = new((float)Math.PI / 2, 0f, 0f);
            this.MoveSpeed = 0.2f;
            this.MouseSensitivity = 0.01f;
            this.LookAt = new(0, 0, 1);
        }

        public Matrix4 GetViewMatrix()
        {
            Vector3 lookat = new();

            lookat.X = (float)(Math.Sin((float)Orientation.X) * Math.Cos((float)Orientation.Y));
            lookat.Y = (float)Math.Sin((float)Orientation.Y);
            lookat.Z = (float)(Math.Cos((float)Orientation.X) * Math.Cos((float)Orientation.Y));
            LookAt = lookat;
            return Matrix4.LookAt(Position, Position + lookat, Vector3.UnitY);
        }
        public void Move(float x, float y, float z)
        {
            Vector3 offset = new();
            Vector3 forward = new((float)Math.Sin((float)Orientation.X), 0, (float)Math.Cos((float)Orientation.X));
            Vector3 right = new(-forward.Z, 0, forward.X);

            offset += x * right;
            offset += y * forward;
            offset.Y += z;

            offset.NormalizeFast();
            offset = Vector3.Multiply(offset, MoveSpeed);

            Position += offset;
        }
        public void AddRotation(float x, float y)
        {
            x *= MouseSensitivity;
            y *= MouseSensitivity;
            Orientation = 
                new
                (
                (Orientation.X + x) % ((float)Math.PI * 2.0f), 
                Math.Max(Math.Min(Orientation.Y + y, (float)Math.PI / 2.0f - 0.1f), (float)-Math.PI / 2.0f + 0.1f), 
                Orientation.Z
                );
        }
    }
}