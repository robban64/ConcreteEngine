#version 420 core

in vec2 TexCoord;

out vec4 FragColor;

layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2D uAlpha;

uniform vec4 uHighlightColor;

@import ubo:MaterialUniform

void main()
{
    float uvRepeat = uMatParams0.y;
    vec2 uv = TexCoord * uvRepeat;

    vec4 baseTex = texture(uTexture, uv);
    float a = baseTex.a;
    if (uMatParams1.w > 0.5) {
        a = texture(uAlpha, uv).r;
    }

    float cutoff = (uMatParams1.w > 0.5) ? 0.25 : 0.05; 
    if (uMatParams1.z > 0.5 && a < cutoff)
        discard;

    FragColor = vec4(baseTex.rgb, 1.0) * uHighlightColor;
}