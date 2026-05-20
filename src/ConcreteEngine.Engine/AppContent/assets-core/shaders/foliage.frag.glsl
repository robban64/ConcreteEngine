#version 420 core

in vec2 TexCoord;
in vec3 FragPos;
in vec4 GrassColor;

in VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec4 GrassColor;
    vec3 N_world;
} fs_in;


out vec4 FragColor;

layout(binding = 0) uniform sampler2D uTexture;

void main()
{
    vec4 baseTex = texture(uTexture, fs_in.TexCoord);
    if (baseTex.a < 0.5) discard;

    FragColor = baseTex * fs_in.GrassColor;
}