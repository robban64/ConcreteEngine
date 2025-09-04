#version 330 core

in vec3 FragPos;
in vec2 TexCoord;
in vec3 Normal;

out vec4 FragColor;

uniform sampler2D uTexture;

uniform float uTexCoordRepeat;

void main()
{
	vec2 tiledCoords = TexCoord * uTexCoordRepeat;
	FragColor = vec4(texture(uTexture, tiledCoords).xyz, 1.0);
}
