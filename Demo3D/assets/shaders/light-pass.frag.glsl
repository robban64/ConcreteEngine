#version 420 core

in vec2 TexCoord;
out vec4 FragColor;

uniform mat4 uViewProj;
uniform vec2 uLightPos;
uniform float uRadius;
uniform vec3 uColor;      // 0..1
uniform float uIntensity; // scalar
uniform float uSoftness;  
uniform int uShape;       // 0=circle, 1=diamond, 2=square-soft

vec2 ScreenToWorld(vec2 uv, mat4 invProj)
{
    // map uv [0,1] to clip [-1,1]
    vec4 clip = vec4(uv * 2.0 - 1.0, 0.0, 1.0);
    vec4 world = invProj * clip;
    return world.xy / world.w;
}


void main()
{
    mat4 invP = inverse(uViewProj);
    vec2 worldPos = ScreenToWorld(TexCoord, invP);

    vec2 d = worldPos - uLightPos;

    float dist;
    if (uShape == 0) {
        dist = length(d);                     // circle
    } else if (uShape == 1) {
        dist = abs(d.x) + abs(d.y);           // diamond (Manhattan)
    } else {
        dist = max(abs(d.x), abs(d.y));       // square (Chebyshev)
    }

    float t = clamp(1.0 - dist / max(uRadius, 1e-5), 0.0, 1.0);

    float atten = pow(t, uSoftness);
    atten = smoothstep(0.0, 1.0, atten);

    vec3 light = uColor * (uIntensity * atten);

    FragColor = vec4(light, 1.0);
}