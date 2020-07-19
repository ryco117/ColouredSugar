#version 450 core

layout (location = 0) in vec4 position;
layout (location = 1) in vec4 velocity;

uniform mat4 projViewMatrix;
uniform bool perspective;

out vec3 outColor;

void main() {
	float speed = length(velocity.xyz);
	if(speed < 0.05) {
		outColor = vec3(0.0, speed*20.0, 0.66);
	} else if(speed < 0.8) {
		outColor = vec3((speed - 0.05)/0.75, 1.0, 0.66 - 0.88*(speed - 0.05));
	} else {
		speed = min(speed, 8.0);
		outColor = vec3((speed - 0.8)/7.2, 1.0 - (speed - 0.8)/7.2, 0);
	}
	outColor = mix(0.5*position.xyz+vec3(0.5, 0.5, 0.5), outColor, 0.725);

	if(perspective) {
		gl_Position = projViewMatrix * vec4(position.xyz, 1.0);
	} else {
		gl_Position = vec4(position.xyz, 1.0);
	}
}