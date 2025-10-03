#version 420 core

in VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec3 N_world;
    vec3 T_world;
    vec3 B_world;
} fs_in;

out vec4 FragColor;

// Lights
#define MAX_LIGHTS 8

struct LightData {
    vec4 color_intensity; // rgb=color, a=intensity
    vec4 pos_range; // xyz=position, w=range
    vec4 dir_type; // xyz=direction, w=type (0=dir,1=point,2=spot)
    vec4 spot_angles; // x=cosInner, y=cosOuter
};

layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2D uNormalTex;
layout(binding = 2) uniform sampler2D uShadowMap;

#include(Frame)
#include(Camera)
#include(DirLight)
#include(Light)
#include(Shadow)
#include(Material)

// Helpers
float sat(float x) {
    return clamp(x, 0.0, 1.0);
}

float rangeFalloff(float dist, float range) {
    if (range <= 0.0) return 1.0;
    float x = sat(1.0 - pow(dist / range, 4.0));
    return x * x;
}

float punctualAttenuation(float dist, float range) {
    float invSq = 1.0 / max(dist * dist, 1e-4);
    return invSq * rangeFalloff(dist, range);
}

float spotSmooth(vec3 L, vec3 dir, float cosInner, float cosOuter) {
    float c = dot(-L, normalize(dir));
    float t = sat((c - cosOuter) / max(cosInner - cosOuter, 1e-4));
    return t * t;
}

void evalLight(in LightData ld, in vec3 P, out vec3 L, out float atten, out vec3 radiance) {
    vec3 color = ld.color_intensity.rgb;
    float intensity = ld.color_intensity.a;

    int type = int(ld.dir_type.w + 0.5);
    if (type == 0) {
        L = normalize(-ld.dir_type.xyz);
        atten = 1.0;
    } else {
        vec3 toL = ld.pos_range.xyz - P;
        float dist = length(toL);
        L = toL / max(dist, 1e-4);
        atten = punctualAttenuation(dist, ld.pos_range.w);

        if (type == 2) {
            float s = spotSmooth(L, ld.dir_type.xyz, ld.spot_angles.x, ld.spot_angles.y);
            atten *= s;
        }
    }
    radiance = color * intensity;
}

// Shadow sampling
float sampleShadowMap(vec4 lsPos, vec3 N, vec3 L) {
    if (uShadowParams1.x <= 0.0) return 1.0; // strength == 0
    vec3 proj = lsPos.xyz / lsPos.w;
    if (proj.x < 0.0 || proj.x > 1.0 || proj.y < 0.0 || proj.y > 1.0)
        return 1.0;
    float current = proj.z;
    float bias = uShadowParams0.z + uShadowParams0.w * (1.0 - dot(N, L));

    float sum = 0.0;
    int radius = int(uShadowParams1.y + 0.5);
    vec2 texel = uShadowParams0.xy;
    for (int x = -radius; x <= radius; ++x) {
        for (int y = -radius; y <= radius; ++y) {
            vec2 offset = vec2(x, y) * texel;
            float closest = texture(uShadowMap, proj.xy + offset).r;
            sum += current - bias <= closest ? 1.0 : 0.0;
        }
    }
    float samples = float((2 * radius + 1) * (2 * radius + 1));
    return sum / samples;
}

// Fog
float computeFogFactor(vec3 P, float viewDist) {
    float fExp2 = 1.0 - exp(-uFogParams0.x * viewDist * viewDist);
    float height = max(0.0, P.y - uFogParams0.z);
    float fHeight = 1.0 - exp(-uFogParams0.y * height);

    float f = uFogParams1.x * fExp2 + uFogParams1.y * fHeight;
    f = clamp(f * uFogParams0.w, 0.0, 1.0);
    if (viewDist > uFogParams1.z) f = 1.0;
    return f;
}

vec3 computeFogColor(vec3 lightColor, float shadowTerm) {
    vec3 cFog = uFogColor.rgb;
    vec3 litFog = cFog + lightColor * shadowTerm;
    return mix(cFog, litFog, uFogColor.a);
}

vec3 hemiAmbient(vec3 N) {
    float up = 0.5 + 0.5 * N.y;
    return mix(uAmbientGround.rgb, uAmbient.rgb, up);
}

float blinnPhongSpec(vec3 N, vec3 V, vec3 L, float shininess) {
    vec3 H = normalize(V + L);
    float ndh = max(dot(N, H), 0.0);
    return pow(ndh, shininess);
}

void main()
{
    vec3 baseColor = texture(uTexture, fs_in.TexCoord).rgb * MaterialColor.rgb;

    vec3 P = fs_in.FragPos;
    vec3 V = normalize(uCameraPos.xyz - P);
    vec3 N = normalize(fs_in.N_world);

    vec3 Lo = vec3(0.0);
    int count = clamp(uLightCounts.x, 0, MAX_LIGHTS);

    for (int i = 0; i < count; ++i) {
        vec3 L, Li;
        float atten;
        evalLight(uLights[i], P, L, atten, Li);

        float NdotL = max(dot(N, L), 0.0);
        if (NdotL <= 0.0) continue;

        vec3 diffuse = baseColor * NdotL;
        float spec = blinnPhongSpec(N, V, L, 64.0);
        vec3 specular = vec3(0.04) * spec;

        float shadowTerm = 1.0;
        if (i == 0 && int(uLights[i].dir_type.w + 0.5) == 0) {
            vec4 lsPos = uLightViewProj * vec4(P, 1.0);
            shadowTerm = sampleShadowMap(lsPos, N, L);
            shadowTerm = mix(1.0, shadowTerm, uShadowParams1.x);
        }

        Lo += (diffuse + specular) * Li * atten * shadowTerm;
    }

    vec3 ambient = hemiAmbient(N) * baseColor;

    float exposure = max(uAmbient.w, 0.0) + 1.0;
    vec3 litColor = (ambient + Lo) * exposure;

    float viewDist = length(uCameraPos.xyz - P);
    float fogF = computeFogFactor(P, viewDist);

    vec3 mainLightColor = (count > 0 && int(uLights[0].dir_type.w + 0.5) == 0)
        ? uLights[0].color_intensity.rgb * uLights[0].color_intensity.a : vec3(0.0);

    vec3 fogColor = computeFogColor(mainLightColor, 1.0);
    vec3 finalColor = mix(litColor, fogColor, fogF);

    FragColor = vec4(finalColor, 1.0);
}
