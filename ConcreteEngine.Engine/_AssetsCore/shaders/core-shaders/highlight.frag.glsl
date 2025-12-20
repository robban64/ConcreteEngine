#version 420 core

in vec2 TexCoord;
in vec3 FragPos;

out vec4 FragColor;

layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2D uAlpha;

uniform vec4 uHighlightColor;

@import ubo:EngineUniform
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
    if (uMatParams1.z > 0.5 && a < cutoff) discard;

    vec4 finalColor = vec4(baseTex.rgb * uHighlightColor.rgb, a);

    float pulseSpeed = 0.25;
    // This gives a 0-1-0 linear triangle wave.
    float triWave = abs(fract(uTime * pulseSpeed) * 2.0 - 1.0);

    float pulseAlpha = mix(0.6, 1.0, triWave);
    finalColor.a *= pulseAlpha;

    float wave1 = sin(uTime * 13.5);
    float wave2 = sin(uTime * 21.1);
    float wave3 = sin(uTime * 37.3);

    float smoothNoise = (wave1 + wave2 + wave3) / 3.0;
    smoothNoise = (smoothNoise + 1.0) * 0.5;// Now 0.0 to 1.0
    float flicker = mix(0.95, 1.0, smoothNoise);

    finalColor.a *= flicker;

    float scrollSpeed = 0.1;
    float lineDensity = 1.2;
    float lineThickness = 0.05;
    float lineIntensity = 0.5;

    float scrollOffset = FragPos.y * lineDensity + uTime * scrollSpeed;
    float scanLine = mod(scrollOffset, 1.0);
    float line = smoothstep(0.0, lineThickness, scanLine) - smoothstep(lineThickness, lineThickness + 0.02, scanLine);
    finalColor.rgb += vec3(0.5, 0.7, 1.0) * line * lineIntensity;

    vec2 screenPos = gl_FragCoord.xy * uInvResolution;
    float dist = distance(screenPos, uMouse);
    float hotspot = 1.0 - smoothstep(0.0, 0.4, dist);
    vec3 hotspotColor = vec3(0.302, 0.678, 0.882);

    finalColor.rgb = mix(finalColor.rgb, hotspotColor, hotspot * 0.3);
    FragColor = finalColor;
}