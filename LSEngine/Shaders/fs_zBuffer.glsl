#version 460

in float distanceFromCamera;
out vec4 outputColor;


float map(float value, float min1, float max1, float min2, float max2) {
  return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
}

void
main()
{
	float d = map(distanceFromCamera,0,1500,0,0.9);
	outputColor = vec4( vec3(1-d), 1.0);
}