#version 450 core
out vec4 FragColor;

uniform vec4 quadColor;

void main()
{
	FragColor = quadColor;
}