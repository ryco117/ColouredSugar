#version 330 core
in vec3 normal;
out vec4 FragColor;

uniform vec3 triColor;

const float ambientStrength = 0.5;
const vec3 lightDir = normalize(vec3(0.0, -1.0, 1.0));
const vec3 lightColour = vec3(1.0);

void main()
{
	// Lighting
    vec3 ambient = ambientStrength * lightColour;
	vec3 diffuse = max(dot(normal, -lightDir), 0.0) * lightColour;
	vec3 resultColor = (ambient + diffuse) * triColor;

	FragColor = vec4(resultColor, 1.0);
}