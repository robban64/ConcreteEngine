#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in vec3 aTangent;

out vec3 FragPos;
out vec2 TexCoord;
out vec3 Normal;

uniform mat4 uModel;
uniform mat3 uNormalMat;
uniform mat4 uViewProj;

void main()
{

	vec4 worldPosition = uModel * vec4(aPos, 1.0);
	FragPos = worldPosition.xyz;
	Normal = uNormalMat * aNormal;
	TexCoord = aTexCoord;

	gl_Position = uViewProj * worldPosition;
}
