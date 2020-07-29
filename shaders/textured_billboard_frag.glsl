#version 450 core
in vec2 exTexCoord;
out vec4 FragColor;

uniform sampler2D textureMap;

void main(void)
{
   FragColor = texture(textureMap, exTexCoord);
}