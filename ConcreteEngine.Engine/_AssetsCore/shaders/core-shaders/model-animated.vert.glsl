#version 420 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in vec4 aTangent;
layout(location = 4) in ivec4 aJointIndices;
layout(location = 5) in vec4 aWeights;

out VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec3 N_world;
    vec3 T_world;
    vec3 B_world;
} vs_out;

@import ubo:CameraUniform
@import ubo:DrawUniform
@import ubo:DrawAnimationUniform

mat3 getNormalMatrix() {
    return mat3(uNormalCol0.xyz, uNormalCol1.xyz, uNormalCol2.xyz);
}

void main() {

    vec4 skinnedPos = vec4(0.0);
    skinnedPos += uJointTransforms[aJointIndices[0]] * vec4(aPos, 1.0) * aWeights[0];
    skinnedPos += uJointTransforms[aJointIndices[1]] * vec4(aPos, 1.0) * aWeights[1];
    skinnedPos += uJointTransforms[aJointIndices[2]] * vec4(aPos, 1.0) * aWeights[2];
    skinnedPos += uJointTransforms[aJointIndices[3]] * vec4(aPos, 1.0) * aWeights[3];

    vec4 worldPos = uModel * skinnedPos;

    mat3 normalMat = getNormalMatrix();

    vs_out.FragPos = worldPos.xyz;
    vs_out.TexCoord = aTexCoord;

    vec3 N = normalize(normalMat * aNormal);
    vec3 T = normalize(normalMat * aTangent.xyz);
    T = normalize(T - N * dot(T, N));
    vec3 B = normalize(cross(N, T)) * aTangent.w;

    vs_out.N_world = N;
    vs_out.T_world = T;
    vs_out.B_world = B;

    gl_Position = uProjViewMat * worldPos;
}
