#version 460

void
main()
{
float z = 1.0 - (gl_FragCoord.z / gl_FragCoord.w) / 5000.0;
gl_FragColor = vec4(z, z, z, 1.0);
}