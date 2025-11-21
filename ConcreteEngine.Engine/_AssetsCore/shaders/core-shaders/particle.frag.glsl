#version 420 core

in vec2 TexCoord;
in vec3 FragPos;
in vec4 ParticleColor;

out vec4 FragColor;

layout(binding = 0) uniform sampler2D uTexture;

void main()
{
    vec4 baseTex = texture(uTexture, TexCoord); 
    if (baseTex.a < 0.5)
        discard;

    FragColor = baseTex * ParticleColor;
}