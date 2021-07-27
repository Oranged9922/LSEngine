#version 460

in vec3 v_norm;
in vec3 v_pos;
in vec2 f_texcoord;
out vec4 outputColor;

uniform sampler2D map_specular;

uniform vec3 material_specular;

void
main()
{
	vec3 n = normalize(v_pos);

	outputColor = vec4( 0.5 + 0.5 * n, 1.0);
}