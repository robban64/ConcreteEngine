#version 420 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;

out VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec2 TexCoordWeight;
    vec3 N_world;
} vs_out;

out vec3 FragPos;
out vec2 TexCoord;
out vec3 Normal;

#include(Frame)
#include(Camera)
#include(DrawObject)

mat3 getNormalMatrix() {
    return mat3(uNormalCol0.xyz, uNormalCol1.xyz, uNormalCol2.xyz);
}

void main()
{
    mat3 normalMat = getNormalMatrix();

    vec4 worldPos = uModel * vec4(aPos, 1.0);
    vs_out.FragPos = worldPos.xyz;
    vs_out.TexCoord = aTexCoord;
    vec2 uvWeight = 1.0 / vec2(256, 256);
    vs_out.TexCoordWeight = (worldPos.xz - 256) * uvWeight;

    vs_out.N_world = normalize(normalMat * aNormal);

    gl_Position = uProjViewMat * worldPos;
}
