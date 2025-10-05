#version 420 core

#define MAX_LIGHTS 8

in VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec2 TexCoordWeight;
    vec3 N_world;
} fs_in;

out vec4 FragColor;

@import struct:LightData

@import ubo:FrameUniform
@import ubo:CameraUniform
@import ubo:DirLightUniform
@import ubo:LightUniform
@import ubo:ShadowUniform
@import ubo:MaterialUniform
@import ubo:DrawUniform

layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2D uAlbedoR;
layout(binding = 2) uniform sampler2D uAlbedoG;
layout(binding = 3) uniform sampler2D uAlbedoB;
layout(binding = 4) uniform sampler2D uWeightMap;

// depth
layout(binding = 5) uniform sampler2D uShadowMap;

float saturate(float x) {
    return clamp(x, 0.0, 1.0);
}

float blinnPhongSpec(vec3 N, vec3 V, vec3 L, float shininess) {
    vec3 H = normalize(V + L);
    return pow(max(dot(N, H), 0.0), shininess);
}

vec3 hemiAmbient(vec3 N) {
    float up = 0.5 + 0.5 * N.y;
    return mix(uAmbientGround.rgb, uAmbient.rgb, up);
}

float rangeFalloff(float dist, float range) {
    if (range <= 0.0) return 1.0;
    float x = saturate(1.0 - pow(dist / range, 4.0));
    return x * x;
}

float punctualAttenuation(float dist, float range) {
    float invSq = 1.0 / max(dist * dist, 1e-4);
    return invSq * rangeFalloff(dist, range);
}

float spotSmooth(vec3 L, vec3 dir, float cosInner, float cosOuter) {
    float c = dot(-L, normalize(dir));
    float t = saturate((c - cosOuter) / max(cosInner - cosOuter, 1e-4));
    return t * t;
}

void evalPunctual(in LightData ld, in vec3 P, out vec3 L, out float atten, out vec3 radiance) {
    vec3 color = ld.color_intensity.rgb;
    float intensity = ld.color_intensity.a;

    int type = int(ld.dir_type.w + 0.5);
    if (type == 1) { // point
        vec3 toL = ld.pos_range.xyz - P;
        float dist = length(toL);
        L = toL / max(dist, 1e-4);
        atten = punctualAttenuation(dist, ld.pos_range.w);
    } else { // spot
        vec3 toL = ld.pos_range.xyz - P;
        float dist = length(toL);
        L = toL / max(dist, 1e-4);
        atten = punctualAttenuation(dist, ld.pos_range.w);
        float s = spotSmooth(L, ld.dir_type.xyz, ld.spot_angles.x, ld.spot_angles.y);
        atten *= s;
    }
    radiance = color * intensity;
}

float computeFogFactor(vec3 P, float viewDist) {
    float fExp2 = 1.0 - exp(-uFogParams0.x * viewDist * viewDist);
    float height = max(0.0, P.y - uFogParams0.z);
    float fHeight = 1.0 - exp(-uFogParams0.y * height);
    float f = uFogParams1.x * fExp2 + uFogParams1.y * fHeight;
    f = clamp(f * uFogParams0.w, 0.0, 1.0);
    if (viewDist > uFogParams1.z) f = 1.0;
    return f;
}

vec3 computeFogColor(vec3 sunColor, float shadowTerm) {
    vec3 cFog = uFogColor.rgb;
    vec3 litFog = cFog + sunColor * shadowTerm;
    return mix(cFog, litFog, uFogColor.a);
}

// Regular shadow map with manual compare + square PCF radius from uShadowParams1.y
float sampleShadowMap(vec4 lightSpacePos, vec3 N, vec3 L)
{
    // Transform to light projection space
    vec3 projCoords = lightSpacePos.xyz / lightSpacePos.w;
    projCoords = projCoords * 0.5 + 0.5; // map from [-1,1] to [0,1]

    // Early out if outside light frustum
    if (projCoords.x < 0.0 || projCoords.x > 1.0 ||
        projCoords.y < 0.0 || projCoords.y > 1.0)
        return 1.0;

    // Bias depending on normal vs light direction
    float cosTheta = clamp(dot(N, L), 0.0, 1.0);
    float normalBias = (1.0 - cosTheta) * 0.01;     // reduces acne on grazing angles
    float bias = uShadowParams0.w + normalBias;

    // Shadow map texel size (for PCF)
    vec2 texelSize = uShadowParams0.xy * uShadowParams0.z;
    float shadow = 0.0;

    // 3×3 PCF
    for (int x = -1; x <= 1; ++x)
    for (int y = -1; y <= 1; ++y)
    {
        vec2 offset = vec2(x, y) * texelSize;
        float depth = texture(uShadowMap, projCoords.xy + offset).r;
        shadow += step(projCoords.z - bias, depth);
    }

    shadow /= 9.0;
    return shadow;
}

vec3 terrainAlbedo(vec2 texCoords, float uvRepeat) {
    vec2 uv = texCoords * uvRepeat;

    vec3 wrgb = texture(uWeightMap, texCoords).rgb;
    float w3 = max(1.0 - (wrgb.r + wrgb.g + wrgb.b), 0.0);

    vec3 c0 = texture(uTexture, uv).rgb;
    vec3 c1 = texture(uAlbedoR, uv).rgb;
    vec3 c2 = texture(uAlbedoG, uv).rgb;
    vec3 c3 = texture(uAlbedoB, uv).rgb;

    vec3 totalColor = c0 * w3 + c1 * wrgb.r + c2 * wrgb.g + c3 * wrgb.b;
    return totalColor;
}

void main() {
    float uvRepeat = uMatParams0.y;

    vec3 baseTex = terrainAlbedo(fs_in.TexCoord, uvRepeat);
    vec3 baseColor = baseTex * uMatColor.rgb;

    vec3 P = fs_in.FragPos;
    vec3 V = normalize(uCameraPos.xyz - P);
    vec3 N = normalize(fs_in.N_world);

    // Directional light (sun)
    vec3 Ld = normalize(-uLightDirection.xyz);
    vec3 LiD = uLightDiffuse.rgb * uLightDiffuse.a;

    float NdotLd = max(dot(N, Ld), 0.0);
    vec3 diffuse = baseColor * NdotLd;

    float shininess = uMatParams1.x;
    float specularStrength = uMatParams0.x;
    float specD = blinnPhongSpec(N, V, Ld, shininess);
    vec3 specular = vec3(specularStrength) * specD * uLightSpecularIntensity.x;

    // Shadow
    float dirShadow = sampleShadowMap(uLightViewProj * vec4(P, 1.0), N, Ld);
    dirShadow = mix(1.0, dirShadow, uShadowParams1.x);

    vec3 direct = (diffuse + specular) * LiD * dirShadow;

    // Point/spot lights
    int lightCount = clamp(uLightCounts.x, 0, MAX_LIGHTS);
    for (int i = 0; i < lightCount; ++i) {
        vec3 Lp, LiP;
        float atten;
        evalPunctual(uLights[i], P, Lp, atten, LiP);

        float NdotLp = max(dot(N, Lp), 0.0);
        if (NdotLp <= 0.0) continue;

        vec3 diffP = baseColor * NdotLp;
        float specP = blinnPhongSpec(N, V, Lp, shininess);
        vec3 specPCol = vec3(specularStrength) * specP * uLightSpecularIntensity.x;

        direct += (diffP + specPCol) * LiP * atten;
    }

    // Ambient + exposure (hemi-ambient)
    vec3 ambient = hemiAmbient(N) * baseColor;
    float exposure = max(uAmbient.w, 0.0) + 1.0;
    vec3 litColor = (ambient + direct) * exposure;

    // Fog
    float viewDist = length(uCameraPos.xyz - P);
    float fogF = computeFogFactor(P, viewDist);
    vec3 fogColor = computeFogColor(LiD, 1.0);

    vec3 finalColor = mix(litColor, fogColor, fogF);
    FragColor = vec4(finalColor, 1.0);
}
