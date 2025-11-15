#version 420 core

uniform vec4 uHighlightColor;

out vec4 FragColor;

void main()
{
    FragColor = uHighlightColor;
}