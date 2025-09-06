#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in vec3 aTangent;

out vec3 FragPos;
out vec2 TexCoord;
out vec3 Normal;

out vec3 N_world;
out vec3 T_world;
out vec3 B_world;

uniform mat4 uModel;
uniform mat3 uNormalMat;
uniform mat4 uViewProj;

void main()
{
	vec4 worldPosition = uModel * vec4(aPos, 1.0);
	FragPos = worldPosition.xyz;
	TexCoord = aTexCoord;

    vec3 N = normalize(uNormalMat * aNormal);
    vec3 T = normalize(uNormalMat * aTangent);
    T = normalize(T - N * dot(T, N));
    vec3 B = normalize(cross(N, T));

    N_world = N;
    T_world = T;
    B_world = B;

	gl_Position = uViewProj * worldPosition;
}
