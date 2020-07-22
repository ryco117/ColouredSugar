#version 450 core

layout (local_size_x = 128, local_size_y = 1, local_size_z = 1) in;

layout (binding = 0) buffer PositionBuffer {
	vec4 positions[];
};
layout (binding = 1) buffer VelocityBuffer {
	vec4 velocities[];
};

// Delta time
uniform float deltaTime;
uniform vec4 attractors[3];
uniform vec4 experimental;
uniform bool perspective;

void main(void)
{
	uint index = gl_GlobalInvocationID.x;

	// Read the current position and velocity from the buffers
	vec4 pos = positions[index];
	vec4 vel = velocities[index];
	
	vec3 g = vec3(0.0);
	float min_length = 0.0125;
	if(perspective) {
		vec3 t = experimental.xyz - pos.xyz;
		float r = max(length(t), min_length);
		g += experimental.w * (normalize(cross(t, pos.xyz)) + normalize(t)/1.35) / (r*r);
		for(int i = 0; i < attractors.length(); i++) {
			t = attractors[i].xyz - pos.xyz;
			r = max(length(t), min_length);
			g += attractors[i].w * normalize(t) / (r*r);
		}
	} else {
		vec3 t = vec3(experimental.xy, pos.z) - pos.xyz;
		float r = max(length(t), min_length);
		g += experimental.w * (normalize(cross(t, pos.xyz)) + normalize(t)/1.35) / (r*r);
		for(int i = 0; i < attractors.length(); i++) {
			t = vec3(attractors[i].xy, pos.z) - pos.xyz;
			r = max(length(t), min_length);
			g += attractors[i].w * normalize(t) / (r*r);
		}
	}
	vel.xyz += deltaTime * g;
	
	if(length(vel.xyz) > 10.0) {
		vel.xyz = 10.0*normalize(vel.xyz);
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

	positions[index] = pos;
	velocities[index] = 0.978 * vel;
}