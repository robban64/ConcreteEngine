#version 420 core

#define MAX_LIGHTS 8

in VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec3 N_world;
    vec3 T_world;
    vec3 B_world;
} fs_in;

out vec4 FragColor;

@import struct:LightData

@import ubo:FrameUniform
@import ubo:CameraUniform
@import ubo:DirLightUniform
@import ubo:LightUniform
@import ubo:ShadowUniform
@import ubo:MaterialUniform


layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2D uNormalTex;
layout(binding = 2) uniform sampler2D uShadowMap;


float saturate(float x) {
    return clamp(x, 0.0, 1.0);
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

float blinnPhongSpec(vec3 N, vec3 V, vec3 L, float shininess) {
    vec3 H = normalize(V + L);
    return pow(max(dot(N, H), 0.0), shininess);
}
vec3 hemiAmbient(vec3 N) {
    float up = 0.5 + 0.5 * N.y;
    return mix(uAmbientGround.rgb, uAmbient.rgb, up);
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

float sampleShadowMap(vec4 lightSpacePos, vec3 N, vec3 L) {
    if (uShadowParams1.x <= 0.0) return 1.0; // off
    vec3 proj = lightSpacePos.xyz / lightSpacePos.w;
    if (proj.x < 0.0 || proj.x > 1.0 || proj.y < 0.0 || proj.y > 1.0) return 1.0;
    float current = proj.z;
    float bias = uShadowParams0.z + uShadowParams0.w * (1.0 - dot(N, L));
    int radius = int(uShadowParams1.y + 0.5);
    vec2 texel = uShadowParams0.xy;
    float sum = 0.0;
    for (int x = -radius; x <= radius; ++x)
        for (int y = -radius; y <= radius; ++y) {
            float closest = texture(uShadowMap, proj.xy + vec2(x, y) * texel).r;
            sum += (current - bias <= closest) ? 1.0 : 0.0;
        }
    float samples = float((2 * radius + 1) * (2 * radius + 1));
    return sum / samples;
}

mat3 makeTBN(vec3 T, vec3 B, vec3 N) {
    vec3 t = normalize(T - N * dot(T, N));
    vec3 b = normalize(cross(N, t));
    vec3 n = normalize(N);
    return mat3(t, b, n);
}

void main()
{
    float uvRepeat = uMatParams0.y;
    vec2 uv = fs_in.TexCoord * uvRepeat;

    vec3 baseTex = texture(uTexture, uv).rgb;
    vec3 baseColor = baseTex * uMatColor.rgb;

    // Normal map (RGB or RGBA8)
    mat3 TBN = makeTBN(fs_in.T_world, fs_in.B_world, fs_in.N_world);
    vec3 nTex = texture(uNormalTex, uv).rgb * 2.0 - 1.0;
    nTex = normalize(nTex);
    vec3 N = normalize(TBN * nTex);

    vec3 P = fs_in.FragPos;
    vec3 V = normalize(uCameraPos.xyz - P);

    // Directional light (sun)
    vec3 Ld = normalize(-uLightDirection.xyz);
    vec3 LiD = uLightDiffuse.rgb * uLightDiffuse.a;

    float NdotLd = max(dot(N, Ld), 0.0);
    vec3 diffuse = baseColor * NdotLd;

    float shininess = uMatParams1.x;
    float specularStrength = uMatParams0.x;
    float specD = blinnPhongSpec(N, V, Ld, shininess);
    vec3 specular = vec3(specularStrength) * specD * uLightSpecularIntensity.x;

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

        vec3 diffuseP = baseColor * NdotLp;
        float specP = blinnPhongSpec(N, V, Lp, shininess);
        vec3 specularP = vec3(specularStrength) * specP * uLightSpecularIntensity.x;

        direct += (diffuseP + specularP) * LiP * atten; // no punctual shadows here
    }

    // Ambient + exposure
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
