#version 420 core

in vec2 TexCoord;
in vec3 FragPos;

out vec4 FragColor;

uniform vec4 uColor;

void main()
{    
    FragColor = vec4(uColor.xyz, 0.4);
}