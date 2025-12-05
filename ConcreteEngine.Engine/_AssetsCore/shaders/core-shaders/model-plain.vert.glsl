#version 420 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;
layout(location = 4) in ivec4 aJointIndices;
layout(location = 5) in vec4 aWeights;

@import ubo:CameraUniform
@import ubo:DrawUniform
@import ubo:DrawAnimationUniform

uniform bool uIsAnimated;

out vec2 TexCoord;
out vec3 FragPos;

void main() {
    TexCoord = aTexCoord;

    vec4 totalLocalPos = vec4(aPos, 1.0);
    if (uIsAnimated) {
        vec4 skinnedPos = vec4(0.0);
        skinnedPos += uJointTransforms[aJointIndices[0]] * vec4(aPos, 1.0) * aWeights[0];
        skinnedPos += uJointTransforms[aJointIndices[1]] * vec4(aPos, 1.0) * aWeights[1];
        skinnedPos += uJointTransforms[aJointIndices[2]] * vec4(aPos, 1.0) * aWeights[2];
        skinnedPos += uJointTransforms[aJointIndices[3]] * vec4(aPos, 1.0) * aWeights[3];
        totalLocalPos = skinnedPos;
    }

    vec4 worldPos = uModel * totalLocalPos;
    FragPos = worldPos.xyz;
    gl_Position = uProjViewMat * worldPos;
}
