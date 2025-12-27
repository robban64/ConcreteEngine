#version 420 core

in vec2 TexCoord;
in vec3 FragPos;

out vec4 FragColor;

uniform vec4 uColor;

@import ubo:EngineUniform

void main()
{
    float scan = sin(FragPos.y * 1.5 - uTime * 2.0);
    float alpha = 0.05 + 0.1 * (scan * 0.5 + 0.5);
    if (alpha < 0.01) discard;
    vec3 color = vec3(0.0, 1.0, 0.2) * 1.5;
    FragColor = vec4(color, 0.5);
}