#version 330 core
layout (location = 0) in vec3 vPosition;

uniform mat4 lightSpaceMatrix;
out vec4 color;

void main()
{
    gl_Position = lightSpaceMatrix *  vec4(vPosition, 1.0);
    color = gl_Position;
}