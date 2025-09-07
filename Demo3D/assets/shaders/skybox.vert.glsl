#version 420 core

layout(location = 0) in vec3 aPos;

out vec3 Pos;

uniform mat4 uView; // view without translation
uniform mat4 uProj;

void main()
{
    Pos = aPos;
    vec4 pos = uProj * uView * vec4(aPos, 1.0);
    gl_Position = vec4(pos.x,pos.y, pos.w, pos.w); // pos.w for depth test
}
