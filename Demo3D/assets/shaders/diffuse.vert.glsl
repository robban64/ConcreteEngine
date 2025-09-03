#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;

out vec3 FragPos;
out vec2 TexCoord;
out vec3 Normal;

uniform mat4 uModel;
uniform mat4 uViewProj;
uniform mat3 uNormalMatrix;

void main()
{

	vec4 worldPosition = uModel * vec4(aPos, 1.0);
	FragPos = worldPosition.xyz;
	Normal = uNormalMatrix * aNormal;
	TexCoord = aTexCoord;

	gl_Position = uViewProj * worldPosition;
}
