#version 420 core

layout(location = 0) in vec2 aLocalPos;
layout(location = 1) in vec2 aTexCoord;

layout (location = 2) in vec4 aInstancePosition;
layout (location = 3) in vec4 aInstanceColor;

out vec3 FragPos;
out vec2 TexCoord;
out vec4 ParticleColor;

@import ubo:EngineUniform
@import ubo:CameraUniform

void main() {
    
    vec3 pos = aInstancePosition.xyz
    + uCameraRight.xyz * aLocalPos.x * aInstancePosition.w
    + uCameraUp.xyz * aLocalPos.y * aInstancePosition.w;

    FragPos = pos;
    TexCoord = aTexCoord;
    ParticleColor = aInstanceColor;

    gl_Position = uProjViewMat * vec4(pos, 1.0);
}
