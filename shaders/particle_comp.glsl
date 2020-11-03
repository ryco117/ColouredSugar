#version 430 core

layout (local_size_x = 128, local_size_y = 1, local_size_z = 1) in;

layout (binding = 0) buffer PositionBuffer {
	vec4 positions[];
};
layout (binding = 1) buffer VelocityBuffer {
	vec4 velocities[];
};
layout (binding = 2) buffer FixedPosBuffer {
	vec4 fixedPositions[];
};

uniform vec4 bigBoomers[2];
uniform vec4 curlAttractors[5];
uniform vec4 attractors[7];

uniform float deltaTime;
uniform bool perspective;
uniform bool fixParticles;
uniform vec4 musicalSphere;
uniform float springCoefficient;

const float maxSpeed = 6.0;
const float min_length = 0.01;
const float friction = -1.35;

vec3 Normalize(vec3 t) {
	if(length(t) < 0.000001) {
		return t;
	}
	return normalize(t);
}

void main(void)
{
	const uint index = gl_GlobalInvocationID.x;

	// Read the current position and velocity from the buffers
	vec4 pos = positions[index];
	vec4 vel = velocities[index];
	
	vec3 g = vec3(0.0);
	if(perspective) {
		for(int i = 0; i < curlAttractors.length(); i++) {
			vec3 t = curlAttractors[i].xyz - pos.xyz;
			float r = max(length(t), min_length);
			g += curlAttractors[i].w * (Normalize(cross(t, pos.xyz)) + Normalize(t)/1.25) / (r*r) * (fixParticles ? 1.7 : 1.0);
		}

		for(int i = 0; i < bigBoomers.length(); i++) {
			vec3 t = bigBoomers[i].xyz - pos.xyz;
			float r = max(length(t), min_length);
			g -= bigBoomers[i].w * Normalize(t) / (r*r*r*r*r) * (fixParticles ? 0.3 : 1.0);
		}

		for(int i = 0; i < attractors.length(); i++) {
			vec3 t = attractors[i].xyz - pos.xyz;
			float r = max(length(t), min_length);
			g += attractors[i].w * Normalize(t) / (r*r) * (fixParticles ? 0.95 : 1.0);
		}
	} else {
		for(int i = 0; i < curlAttractors.length(); i++) {
			vec3 t = vec3(curlAttractors[i].xy, pos.z) - pos.xyz;
			float r = max(length(t), min_length);
			g += curlAttractors[i].w * (Normalize(cross(t, pos.xyz)) + Normalize(t)/1.25) / (r*r) * (fixParticles ? 1.7 : 1.0);
		}

		for(int i = 0; i < bigBoomers.length(); i++) {
			vec3 t = vec3(bigBoomers[i].xy, pos.z) - pos.xyz;
			float r = max(length(t), min_length);
			g -= bigBoomers[i].w * Normalize(t) / (r*r*r)  * (fixParticles ? 0.3 : 1.0);
		}

		for(int i = 0; i < attractors.length(); i++) {
			vec3 t = vec3(attractors[i].xy, pos.z) - pos.xyz;
			float r = max(length(t), min_length);
			g += attractors[i].w * Normalize(t) / (r*r) * (fixParticles ? 0.95 : 1.0);
		}

		// Scale 2D forces down (to account for smaller distances)
		g *= 0.175;
	}

	if(fixParticles) {
		g += springCoefficient * (fixedPositions[index].xyz - pos.xyz);
	}

	vel.xyz += deltaTime * g;
	
	if(length(vel.xyz) > maxSpeed) {
		vel.xyz = maxSpeed*normalize(vel.xyz);
	}

	pos.xyz += vel.xyz * deltaTime;
	if(abs(pos.x) > 1.0) {
		vel.x = sign(pos.x) * (-0.95 * abs(vel.x) - 0.0001);
		if(abs(pos.x) >= 1.05) {
			pos.x = sign(pos.x);
		}
	}
	if(abs(pos.y) > 1.0) {
		vel.y = sign(pos.y) * (-0.95 * abs(vel.y) - 0.0001);
		if(abs(pos.y) >= 1.05) {
			pos.y = sign(pos.y);
		}
	}
	if(abs(pos.z) > 1.0) {
		vel.z = sign(pos.z) * (-0.95 * abs(vel.z) - 0.0001);
		if(abs(pos.z) >= 1.05) {
			pos.z = sign(pos.z);
		}
	}

	// Musical Spheres
	vec3 d = perspective ? pos.xyz - musicalSphere.xyz : vec3(pos.xy - musicalSphere.xy, 0.0);
	float mag = max(length(d), min_length);
	if(mag <= musicalSphere.w) {
		pos.xyz = musicalSphere.xyz + (musicalSphere.w/mag)*d;
		vel.xyz += 20.0 * (musicalSphere.w - mag) * Normalize(d);
	}

	positions[index] = pos;
	velocities[index] =  vel * exp(friction * deltaTime);
}