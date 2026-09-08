#version 420 core

layout(location = 0) in vec2 aLocalPos;
layout(location = 1) in vec2 aTexCoord;

layout (location = 2) in vec4 aInstancePosition;
layout (location = 3) in float aInstanceSize;
layout (location = 4) in vec4 aInstanceColor;

out vec3 FragPos;
out vec2 TexCoord;
out vec4 ParticleColor;

@import ubo:EngineUniform
@import ubo:CameraUniform
@import ubo:DrawUniform

void main() {

    vec3 translation = uModel[3].xyz;
    vec3 pos = aInstancePosition.xyz + translation
    + uCameraRight.xyz * aLocalPos.x * aInstanceSize
    + uCameraUp.xyz * aLocalPos.y * aInstanceSize;

    FragPos = pos;
    TexCoord = aTexCoord;
    ParticleColor = aInstanceColor;

    gl_Position = uProjViewMat * vec4(pos, 1.0);
}
