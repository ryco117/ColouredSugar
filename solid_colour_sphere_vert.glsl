#version 450 core
layout (location = 0) in vec3 position;
out vec3 normal;

uniform mat4 projViewModel;

void main()
{
	normal = normalize(position);
	gl_Position = vec4(position, 1.0) * projViewModel;
}