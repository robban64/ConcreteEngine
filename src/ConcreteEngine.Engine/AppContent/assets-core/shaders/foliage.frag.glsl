#version 420 core

in VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec3 N_world;
    vec4 FoliageColor;
} fs_in;

out vec4 FragColor;

@import ubo:FrameUniform
@import ubo:CameraUniform
@import ubo:DirLightUniform
@import ubo:ShadowUniform
@import ubo:MaterialUniform

layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2DShadow uShadowMap;

const vec2 offsets[4] = vec2[](
vec2(-0.5, -0.5),
vec2(0.5, -0.5),
vec2(-0.5, 0.5),
vec2(0.5, 0.5)
);


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

float sampleShadowMap(vec4 lightSpacePos, vec3 N, vec3 L)
{
    vec3 p = lightSpacePos.xyz / lightSpacePos.w;
    p = p * 0.5 + 0.5;

    if (p.x < 0.0 || p.x > 1.0 || p.y < 0.0 || p.y > 1.0 || p.z > 1.0)
    return 1.0;

    float ndl  = clamp(dot(N, L), 0.0, 1.0);
    float bias = max(uShadowParams0.z, uShadowParams0.w * (1.0 - ndl));
    float depthToCompare = p.z - bias;

    float shadowSum = 0.0;
    vec2 texelSize = uShadowParams0.xy;
    for (int i = 0; i < 4; ++i)
    {
        shadowSum += texture(uShadowMap, vec3(p.xy + offsets[i] * texelSize, depthToCompare));
    }

    return shadowSum * 0.25;
}

void main()
{
    vec4 baseTex = texture(uTexture, fs_in.TexCoord);
    if (baseTex.a < 0.5) discard;

    vec3 baseColor = baseTex.rgb * fs_in.FoliageColor.rgb;

    vec3 N = normalize(fs_in.N_world); 
    vec3 Ld = normalize(-uLightDirection.xyz);
    vec3 LiD = uLightDiffuse.rgb * uLightDiffuse.a;

    float ndl = dot(N, Ld);
    float diffTerm = ndl * 0.5 + 0.5;
    vec3 diffuse = baseColor * diffTerm;

    float vis = sampleShadowMap(uLightViewProj * vec4(fs_in.FragPos, 1.0), N, Ld);
    float dirShadow = mix(1.0, max(vis, 0.2), uShadowParams1.x);

    vec3 direct = diffuse * LiD * dirShadow;

    float up = clamp(1.0 - fs_in.TexCoord.y, 0.0, 1.0); 
    vec3 ambient = mix(uAmbientGround.rgb, uAmbient.rgb, up) * baseColor;

    float exposure = max(uAmbient.w, 0.0) + 1.0;
    vec3 litColor = (ambient + direct) * exposure;

    float viewDist = length(uCameraPos.xyz - fs_in.FragPos);
    float fogF = computeFogFactor(fs_in.FragPos, viewDist);
    vec3 fogColor = computeFogColor(LiD, 1.0);

    vec3 finalColor = mix(litColor, fogColor, fogF);

    FragColor = vec4(finalColor, 1.0);
}

