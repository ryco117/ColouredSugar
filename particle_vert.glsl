#version 450 core

layout (location = 0) in vec4 position;
layout (location = 1) in vec4 velocity;

uniform mat4 projViewMatrix;
uniform bool perspective;

out vec4 outColor;

void main() {
	float speed = min(length(velocity.xyz), 8.0);
	if(speed < 0.05) {
		outColor = vec4(0.0, speed*20.0, 0.33, 1.0);
		outColor = vec4(mix(0.2*position.xyz+vec3(0.2, 0.2, 0.2), outColor.xyz, speed / 0.05), 1.0);
		//outColor = mix(vec4(0.0), outColor, speed / 0.05);
	} else if(speed < 0.8) {
		outColor = vec4((speed - 0.05)/0.75, 1.0, 0.66 - 0.88*(speed - 0.05), 1.0);
	} else {
		outColor = vec4((speed - 0.8)/7.2, 1.0 - (speed - 0.8)/7.2, 0, 1.0);
	}

	if(perspective) {
		gl_Position = projViewMatrix * vec4(position.xyz, 1.0);
	} else {
		gl_Position = vec4(position.xyz, 1.0);
	}
}