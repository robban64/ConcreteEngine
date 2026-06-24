#version 420 core

in vec2 TexCoord;

@import ubo:MaterialUniform

layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2D uAlpha;

void main()
{
    float uvRepeat = uMat.UvTransform.w;
    vec2 uv = TexCoord * uvRepeat;

    float a = (uMat.AlphaMaskToggle == 1) ? texture(uAlpha, uv).r : texture(uTexture, uv).a;
    if (a < uMat.AlphaCutoff) discard;
}
