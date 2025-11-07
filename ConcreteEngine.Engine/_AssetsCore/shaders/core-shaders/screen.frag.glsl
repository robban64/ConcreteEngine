#version 420 core

in vec2 TexCoord;
out vec4 FragColor;

layout(binding = 0) uniform sampler2D uTexture;

void main()
{
    FragColor = texture(uTexture, TexCoord);
}