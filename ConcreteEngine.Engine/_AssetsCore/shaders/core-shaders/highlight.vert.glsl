#version 420 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;

@import ubo:CameraUniform
@import ubo:DrawUniform

out vec2 TexCoord;

void main() {
    vec4 worldPos = uModel * vec4(aPos, 1.0);
    TexCoord = aTexCoord;
    gl_Position = uProjViewMat * worldPos;
}
