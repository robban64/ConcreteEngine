#version 420 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;

out VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec3 N_world;
} vs_out;

out vec3 FragPos;
out vec2 TexCoord;
out vec3 Normal;

@import ubo:FrameUniform
@import ubo:CameraUniform
@import ubo:DrawUniform

mat3 getNormalMatrix() {
    return mat3(uNormalCol0.xyz, uNormalCol1.xyz, uNormalCol2.xyz);
}

void main()
{
    mat3 normalMat = getNormalMatrix();

    vec4 worldPos = uModel * vec4(aPos, 1.0);
    vs_out.FragPos = worldPos.xyz;
    vs_out.TexCoord = aTexCoord;

    vs_out.N_world = normalize(normalMat * aNormal);

    gl_Position = uProjViewMat * worldPos;
}
