#version 420 core

in vec2 TexCoord;

@import ubo:MaterialUniform

layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2D uAlpha;

void main()
{
    float uvRepeat = uMatParams0.y;
    vec2 uv = TexCoord * uvRepeat;

    // default alpha from main texture
    float a = texture(uTexture, uv).a;

    // override alpha from separate mask
    if (uMatParams1.w > 0.5)
        a = texture(uAlpha, uv).r;

    float cutoff = (uMatParams1.w > 0.5) ? 0.25 : 0.05; 
    if (uMatParams1.z > 0.5 && a < cutoff)
        discard;
}
