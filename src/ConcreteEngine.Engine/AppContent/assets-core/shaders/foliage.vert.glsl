#version 420 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in vec4 aTangent;

layout (location = 2) in vec4 aInstancePosition;
layout (location = 3) in vec4 aInstanceColor;

out vec3 FragPos;
out vec2 TexCoord;
out vec4 GrassColor;

@import ubo:EngineUniform
@import ubo:CameraUniform

void main() {

    vec3 pos = aInstancePosition.xyz + aPos;

    FragPos = pos;
    TexCoord = aTexCoord;
    GrassColor = aInstanceColor;

    gl_Position = uProjViewMat * vec4(pos, 1.0);
}
