#version 430 core

layout (location = 0) in vec4 position;
layout (location = 1) in vec4 velocity;

uniform mat4 projViewMatrix;
uniform bool perspective;

out vec4 outColor;

const float c1 = 0.08;
const vec3 endC1 = vec3(0.0, 0.6, 0.6);
const float c2 = 0.35;
const vec3 endC2 = vec3(0.5, 0.8, 0.0);
const float c3 = 3.0;
const vec3 endC3 = vec3(0.7, 0.0, 1.0);
const float maxSpeed = 8.0;

void main() {
	float speed = min(length(velocity.xyz), maxSpeed);
	if(speed < c1) {
		outColor = vec4(mix(0.2*position.xyz+vec3(0.2), vec3(endC1.x, endC1.y * speed/c1, endC1.z), speed / c1), 1.0);
	} else if(speed < c2) {
		outColor = vec4(mix(endC1, endC2, (speed - c1)/(c2 - c1)), 1.0);
	} else if(speed < c3) {
		outColor = vec4(mix(endC2, endC3, (speed - c2)/(c3 - c2)), 1.0);
	} else {
		outColor = vec4(mix(endC3, vec3(1.0, 0.0, 0.0), (speed - c3)/(maxSpeed - c3)), 1.0);
	}

	gl_Position = vec4(position.xyz, 1.0) * projViewMatrix;
}