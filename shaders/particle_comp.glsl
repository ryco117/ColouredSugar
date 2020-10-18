#version 430 core

layout (local_size_x = 128, local_size_y = 1, local_size_z = 1) in;

layout (binding = 0) buffer PositionBuffer {
	vec4 positions[];
};
layout (binding = 1) buffer VelocityBuffer {
	vec4 velocities[];
};

uniform vec4 bigBoomers[3];
uniform vec4 curlAttractors[5];
uniform vec4 attractors[7];

uniform float deltaTime;
uniform bool perspective;
uniform vec4 musicalSphere;

const float maxSpeed = 10.0;
const float min_length = 0.01;
const float friction = -1.001;

void main(void)
{
	uint index = gl_GlobalInvocationID.x;

	// Read the current position and velocity from the buffers
	vec4 pos = positions[index];
	vec4 vel = velocities[index];
	
	vec3 g = vec3(0.0);
	if(perspective) {
		for(int i = 0; i < curlAttractors.length(); i++) {
			vec3 t = curlAttractors[i].xyz - pos.xyz;
			float r = max(length(t), min_length);
			g += curlAttractors[i].w * (normalize(cross(t, pos.xyz)) + normalize(t)/1.25) / (r*r);
		}

		for(int i = 0; i < bigBoomers.length(); i++) {
			vec3 t = bigBoomers[i].xyz - pos.xyz;
			float r = max(length(t), min_length);
			g -= bigBoomers[i].w * normalize(t) / (r*r*r*r*r);
		}

		for(int i = 0; i < attractors.length(); i++) {
			vec3 t = attractors[i].xyz - pos.xyz;
			float r = max(length(t), min_length);
			g += attractors[i].w * normalize(t) / (r*r);
		}
	} else {
		for(int i = 0; i < curlAttractors.length(); i++) {
			vec3 t = vec3(curlAttractors[i].xy, pos.z) - pos.xyz;
			float r = max(length(t), min_length);
			g += curlAttractors[i].w * (normalize(cross(t, pos.xyz)) + normalize(t)/1.25) / (r*r);
		}

		for(int i = 0; i < bigBoomers.length(); i++) {
			vec3 t = vec3(bigBoomers[i].xy, pos.z) - pos.xyz;
			float r = max(length(t), min_length);
			g -= bigBoomers[i].w * normalize(t) / (r*r*r);
		}

		for(int i = 0; i < attractors.length(); i++) {
			vec3 t = vec3(attractors[i].xy, pos.z) - pos.xyz;
			float r = max(length(t), min_length);
			g += attractors[i].w * normalize(t) / (r*r);
		}

		// Scale 2D forces down (to account for smaller distances)
		g *= 0.75;
	}
	vel.xyz += deltaTime * g;
	
	if(length(vel.xyz) > maxSpeed) {
		vel.xyz = maxSpeed*normalize(vel.xyz);
	}

	pos += vel * deltaTime;
	if(abs(pos.x) >= 0.98) {
		vel.x = sign(pos.x) * (-0.99 * abs(vel.x) - 0.005);
		if(abs(pos.x) >= 0.99) {
			pos.x = sign(pos.x) * 0.98;
		}
	}
	if(abs(pos.y) >= 0.98) {
		vel.y = sign(pos.y) * (-0.99 * abs(vel.y) - 0.005);
		if(abs(pos.y) >= 0.99) {
			pos.y = sign(pos.y) * 0.98;
		}
	}
	if(abs(pos.z) >= 0.98) {
		vel.z = sign(pos.z) * (-0.99 * abs(vel.z) - 0.005);
		if(abs(pos.z) >= 0.99) {
			pos.z = sign(pos.z) * 0.98;
		}
	}

	// Musical Spheres
	vec3 d = perspective ? pos.xyz - musicalSphere.xyz : vec3(pos.xy - musicalSphere.xy, 0.0);
	float mag = length(d);
	if(mag <= musicalSphere.w) {
		pos.xyz = musicalSphere.xyz + (musicalSphere.w/mag)*d;
		vel.xyz += 20.0 * (musicalSphere.w - mag) * normalize(d);
	}

	positions[index] = pos;
	velocities[index] =  vel * exp(friction * deltaTime);
}