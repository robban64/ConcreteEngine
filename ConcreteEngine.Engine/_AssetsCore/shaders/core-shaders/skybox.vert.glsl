#version 420 core

layout(location = 0) in vec3 aPos;

out vec3 Pos;

@import ubo:CameraUniform

void main()
{
    mat4 viewNoTrans = mat4(mat3(uViewMat));

    Pos = aPos;
    vec4 pos = uProjMat * viewNoTrans * vec4(aPos, 1.0);
    gl_Position = vec4(pos.x,pos.y, pos.w, pos.w); // pos.w for depth test
}
