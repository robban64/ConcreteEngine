#version 420 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;

out vec2 TexCoord;

@import ubo:CameraUniform
@import ubo:DrawUniform

void main() {
    TexCoord = aTexCoord;
    vec4 worldPos = uModel * vec4(aPos, 1.0);
    gl_Position = uProjViewMat * worldPos;

}
