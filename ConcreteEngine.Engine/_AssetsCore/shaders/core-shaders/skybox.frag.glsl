#version 420 core

in vec3 Pos;
out vec4 FragColor;

layout(binding = 0) uniform samplerCube uCubemapTex;

void main()
{
    vec4 c = texture(uCubemapTex, normalize(Pos));
    float l = dot(c.rgb, vec3(0.333));
    float t = smoothstep(0.6, 1.0, l);
    c.rgb = mix(c.rgb, vec3(0.6), t * 0.35);
    FragColor = c;
}