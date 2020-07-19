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
uniform vec4 blackHole;
uniform bool perspective;

void main(void)
{
	uint index = gl_GlobalInvocationID.x;

	// Read the current position and velocity from the buffers
	vec4 pos = positions[index];
	vec4 vel = velocities[index];
	
	vec3 g;
	if(perspective) {
		g = blackHole.xyz - pos.xyz;
	} else {
		g = vec3(blackHole.xy, pos.z) - pos.xyz;
	}
	float r = max(length(g), 0.008);
	vel.xyz += blackHole.w * normalize(g) * deltaTime / (r*r);
	
	if(length(vel.xyz) > 10.0) {
		vel.xyz = 10.0*normalize(vel.xyz);
	}

	pos += vel * deltaTime;
	if(abs(pos.x) >= 0.95) {
		vel.x = sign(pos.x) * (-0.725 * abs(vel.x) - 0.004);
		if(abs(pos.x) >= 0.99) {
			pos.x = sign(pos.x) * 0.98;
		}
	}
	if(abs(pos.y) >= 0.95) {
		vel.y = sign(pos.y) * (-0.725 * abs(vel.y) - 0.004);
		if(abs(pos.y) >= 0.99) {
			pos.y = sign(pos.y) * 0.98;
		}
	}
	if(abs(pos.z) >= 0.95) {
		vel.z = sign(pos.z) * (-0.725 * abs(vel.z) - 0.004);
		if(abs(pos.z) >= 0.99) {
			pos.z = sign(pos.z) * 0.98;
		}
	}

	positions[index] = pos;
	velocities[index] = 0.98 * vel;
}