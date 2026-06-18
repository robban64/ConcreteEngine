#version 420 core

in vec2 TexCoord;

@import ubo:MaterialUniform

layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2D uAlpha;

void main()
{
    float uvRepeat = uMatParams1.y;
    vec2 uv = TexCoord * uvRepeat;

    float a = (uMatParams1.w > 0.5) ? texture(uAlpha, uv).r : texture(uTexture, uv).a;
    float cutoff = uMatParams1.z;
    if (a < cutoff) discard;
}
