#version 330 core

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;

out vec4 FragColor;

uniform sampler2D uTexture;

void main()
{
	FragColor = vec4(texture(uTexture, TexCoord).xyz, 1.0);
}
