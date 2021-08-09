#version 330 core
layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 texcoord;
/*
in vec3 vPosition;
in vec3 vNormal;
in vec2 texcoord;
*/
out vec2 TexCoords;
out vec3 lightPosClipSpace;
out vec3 viewPosClipSpace;

out VS_OUT {
    vec3 FragPos;
    vec3 Normal;
    vec2 TexCoords;
    vec4 FragPosLightSpace;
} vs_out;

//uniform mat4 modelviewprojection;
uniform mat4 view;
uniform mat4 model;
uniform mat4 modelview;
uniform mat4 lightSpaceMatrix;
uniform mat4 viewprojectionLight;
uniform mat4 viewLight;

uniform vec3 lightPos;
uniform vec3 viewPos;

void main()
{
    vs_out.FragPos = vec3(model * vec4(vPosition, 1.0));
    vs_out.Normal = transpose(inverse(mat3(model))) * vNormal;
    vs_out.TexCoords = texcoord;
    vs_out.FragPosLightSpace = lightSpaceMatrix * vec4(vs_out.FragPos, 1.0);
    gl_Position = modelview * vec4(vPosition,1.0);
}