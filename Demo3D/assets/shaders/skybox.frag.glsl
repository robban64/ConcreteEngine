#version 420 core

in vec3 Pos;
out vec4 FragColor;

layout(binding = 0) uniform samplerCube uCubemapTex;

void main()
{
    FragColor = texture(uCubemapTex, normalize(Pos));
}