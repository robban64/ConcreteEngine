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
layout(binding = 2) uniform sampler2D uAlpha;
layout(binding = 3) uniform sampler2DShadow uShadowMap;


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

// hardware 2x2 PCF
float sampleShadowMap(vec4 lightSpacePos, vec3 N, vec3 L)
{
    if (uShadowParams1.x <= 0.0) return 1.0;

    vec3 p = lightSpacePos.xyz / lightSpacePos.w;
    p = p * 0.5 + 0.5;

    if (p.x <= 0.0 || p.x >= 1.0 || p.y <= 0.0 || p.y >= 1.0) return 1.0;
    if (p.z >= 1.0) return 1.0;

    float ndl  = max(dot(N, L), 0.0);
    float bias = max(uShadowParams0.z, uShadowParams0.w * (1.0 - ndl)); 
    float ref  = clamp(p.z - bias, 0.0, 1.0);

    float vis    = texture(uShadowMap, vec3(p.xy, ref));
    float shadow = mix(1.0, max(vis, 0.2), 0.9);
    return shadow; 
}


float halfLambert(float ndl) {
    float x = ndl * 0.5 + 0.5;
    x = max(x, 0.0);
    return x * x;
}

void main()
{
    float uvRepeat = uMatParams0.y;
    vec2 uv = fs_in.TexCoord * uvRepeat;

    // Albedo (linear after sampler's sRGB decode)
    vec4 baseTex = texture(uTexture, uv);
    float a = baseTex.a;
    if (uMatParams1.z > 0.9) {
        a = texture(uAlpha, uv).r;
    }
    if (a < 0.4) discard;

    vec3 baseColor = baseTex.rgb * uMatColor.rgb;

    // TBN (world space)
    vec3 Nw = normalize(fs_in.N_world);
    vec3 Tw = normalize(fs_in.T_world);
    Tw = normalize(Tw - Nw * dot(Nw, Tw));
    vec3 Bw = normalize(cross(Nw, Tw));
    mat3 TBN = mat3(Tw, Bw, Nw);

    // Normal map (linear)
    vec3 nTex = texture(uNormalTex, uv).rgb * 2.0 - 1.0;
    vec3 N = normalize(TBN * nTex);

    // Positions & vectors
    vec3 P = fs_in.FragPos;
    vec3 V = normalize(uCameraPos.xyz - P);

    // Directional light (sun)
    vec3 Ld = normalize(-uLightDirection.xyz);
    vec3 LiD = uLightDiffuse.rgb * uLightDiffuse.a;

    // Diffuse (Half-Lambert)
    float ndl = dot(N, Ld);
    float diffTerm = halfLambert(ndl);
    vec3 diffuse = baseColor * diffTerm;

    // Specular (Blinn-Phong)
    float shininess = uMatParams1.x;
    float specularStrength = uMatParams0.x;
    float specD = blinnPhongSpec(N, V, Ld, shininess);
    vec3 specular = vec3(specularStrength) * specD * uLightSpecularIntensity.x;

    // Shadow
    float dirShadow = sampleShadowMap(uLightViewProj * vec4(P + normalize(fs_in.N_world) * uShadowParams1.z, 1.0), normalize(fs_in.N_world), Ld);
    dirShadow = mix(1.0, dirShadow, uShadowParams1.x);

    float dirShadowSpec = max(dirShadow, 0.25);

    // Compose direct (dir light)
    vec3 direct = diffuse * LiD * dirShadow + specular * LiD * dirShadowSpec;

    // Point/spot lights (no shadows here)
    int lightCount = clamp(uLightCounts.x, 0, MAX_LIGHTS);
    for (int i = 0; i < lightCount; ++i) {
        vec3 Lp, LiP; float atten;
        evalPunctual(uLights[i], P, Lp, atten, LiP);

        float ndlp = dot(N, Lp);
        float diffP = halfLambert(ndlp);
        if (diffP <= 0.0) continue;

        vec3 diffuseP = baseColor * diffP;
        float specP = blinnPhongSpec(N, V, Lp, shininess);
        vec3 specularP = vec3(specularStrength) * specP * uLightSpecularIntensity.x;

        direct += (diffuseP + specularP) * LiP * atten;
    }

    // Ambient
    float up = clamp(N.y * 0.5 + 0.5, 0.0, 1.0);
    vec3 ambient = mix(uAmbientGround.rgb, uAmbient.rgb, up) * baseColor;

    // Exposure (ambient.w as exposure-1)
    float exposure = max(uAmbient.w, 0.0) + 1.0;
    vec3 litColor = (ambient + direct) * exposure;

    // Fog
    float viewDist = length(uCameraPos.xyz - P);
    float fogF = computeFogFactor(P, viewDist);
    vec3 fogColor = computeFogColor(LiD, 1.0);

    vec3 finalColor = mix(litColor, fogColor, fogF);

    FragColor = vec4(finalColor, 1.0);
}