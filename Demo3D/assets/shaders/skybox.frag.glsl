#version 330 core
in vec3 Pos;
out vec4 FragColor;

uniform samplerCube uCubemapTex;

void main()
{
    FragColor = texture(uCubemapTex, normalize(Pos));
}