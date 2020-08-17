#version 430 core

layout (local_size_x = 128, local_size_y = 1, local_size_z = 1) in;

layout (binding = 0) buffer PositionBuffer {
	vec4 positions[];
};
layout (binding = 1) buffer VelocityBuffer {
	vec4 velocities[];
};

// Delta time
uniform float deltaTime;
uniform vec4 attractors[2];
uniform vec4 curlAttractor;
uniform vec4 bigBoomer;
uniform bool perspective;
uniform vec4 musicalSphere;

const float maxSpeed = 10.0;
const float min_length = 0.01;
const float friction = 1.4;
const float invfriction = 1 / friction;

void main(void)
{
	uint index = gl_GlobalInvocationID.x;

	// Read the current position and velocity from the buffers
	vec4 pos = positions[index];
	vec4 vel = velocities[index];
	
	vec3 g = vec3(0.0);
	if(perspective) {
		vec3 t = curlAttractor.xyz - pos.xyz;
		float r = max(length(t), min_length);
		g += curlAttractor.w * (normalize(cross(t, pos.xyz)) + normalize(t)/1.5) / (r*r);

		t = bigBoomer.xyz - pos.xyz;
		r = max(length(t), min_length);
		g -= bigBoomer.w * normalize(t) / (r*r*r*r*r);

		for(int i = 0; i < attractors.length(); i++) {
			t = attractors[i].xyz - pos.xyz;
			r = max(length(t), min_length);
			g += attractors[i].w * normalize(t) / (r*r);
		}
	} else {
		vec3 t = vec3(curlAttractor.xy, pos.z) - pos.xyz;
		float r = max(length(t), min_length);
		g += curlAttractor.w * (normalize(cross(t, pos.xyz)) + normalize(t)/1.5) / (r*r);

		t = vec3(bigBoomer.xy, pos.z) - pos.xyz;
		r = max(length(t), min_length);
		g -= bigBoomer.w * normalize(t) / (r*r*r);

		for(int i = 0; i < attractors.length(); i++) {
			t = vec3(attractors[i].xy, pos.z) - pos.xyz;
			r = max(length(t), min_length);
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
	if(abs(pos.x) >= 0.95) {
		vel.x = sign(pos.x) * (-0.99 * abs(vel.x) - 0.005);
		if(abs(pos.x) >= 0.99) {
			pos.x = sign(pos.x) * 0.98;
		}
	}
	if(abs(pos.y) >= 0.95) {
		vel.y = sign(pos.y) * (-0.99 * abs(vel.y) - 0.005);
		if(abs(pos.y) >= 0.99) {
			pos.y = sign(pos.y) * 0.98;
		}
	}
	if(abs(pos.z) >= 0.95) {
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
	velocities[index] =  vel * (1 - friction * min(deltaTime, invfriction));
}