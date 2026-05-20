#version 420 core

in VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec3 N_world;
    vec4 FoliageColor;
} fs_in;


out vec4 FragColor;

layout(binding = 0) uniform sampler2D uTexture;

void main()
{
    vec4 baseTex = texture(uTexture, fs_in.TexCoord);
    if (baseTex.a < 0.5) discard;

    FragColor = baseTex * fs_in.FoliageColor;
}