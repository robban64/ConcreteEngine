#version 420 core

layout(location = 0) in vec3 aPos;

@import ubo:CameraUniform
@import ubo:DrawUniform

void main() {
    vec4 worldPos = uModel * vec4(aPos, 1.0);
    gl_Position = uProjViewMat * worldPos;
}
