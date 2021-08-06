#version 460

layout(location = 0) out vec4 outputColor;

void
main()
{
float z = (gl_FragCoord.z / gl_FragCoord.w) / 3000.0;
//gl_FragColor = vec4(z, z, z, 1.0);
outputColor = vec4(z,z,z,1);
}