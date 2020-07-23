#version 450 core

layout (location = 0) in vec4 position;
layout (location = 1) in vec4 velocity;

uniform mat4 projViewMatrix;
uniform bool perspective;

out vec4 outColor;

const float c1 = 0.05;
const float c2 = 0.4;
const float maxSpeed = 5.0;

void main() {
	float speed = min(length(velocity.xyz), maxSpeed);
	if(speed < c1) {
		outColor = vec4(0.0, speed*(1.0/c1), 0.66, 1.0);
		outColor = vec4(mix(0.2*position.xyz+vec3(0.2, 0.2, 0.2), outColor.xyz, speed / c1), 1.0);
		//outColor = mix(vec4(0.0), outColor, speed / c1);
	} else if(speed < c2) {
		outColor = vec4((speed - c1)/(c2 - c1), 1.0, 0.66 - 0.66*(speed - c1)/(c2 - c1), 1.0);
	} else {
		outColor = vec4((speed - c2)/(maxSpeed - c2), 1.0 - (speed - c2)/(maxSpeed - c2), 0, 1.0);
	}

	if(perspective) {
		gl_Position = projViewMatrix * vec4(position.xyz, 1.0);
	} else {
		gl_Position = vec4(position.xyz, 1.0);
	}
}