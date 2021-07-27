#version 460

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
out float distanceFromCamera;

uniform vec3 cameraPosition;
uniform mat4 modelview;

void
main()
{
    gl_Position = modelview * vec4(vPosition, 1.0);
    distanceFromCamera =  distance(vPosition,cameraPosition);
}