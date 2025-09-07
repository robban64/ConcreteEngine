#version 420 core

in vec3 FragPos;
in vec2 TexCoord;

in vec3 N_world;
in vec3 T_world;
in vec3 B_world;

out vec4 FragColor;

// UBO - (std140)

// FrameGlobalUniformGpuData
layout(std140, binding = 0) uniform FrameGlobalUniform {
    vec4 uAmbient;   // xyz=color, w=intensity
    vec4 uFogColor;  // xyz=color, w=density
    vec4 uFogDetail; // x=near, y=far, z=type, w=0
};

// CameraUniformGpuData
layout(std140, binding = 1) uniform CameraUniform {
    mat4 uViewMat;
    mat4 uProjMat;
    mat4 uProjViewMat;
    vec4 uCameraPos; // .xyz used
};

// DirLightUniformGpuData
layout(std140, binding = 2) uniform DirLightUniform {
    vec4 uLightDirection;            // xyz, w unused
    vec4 uLightDiffuse;              // rgb, w unused
    vec4 uLightSpecularIntensity;    // xyz=specular, w=intensity
};

// MaterialUniformGpuData (see padding note below)
layout(std140, binding = 3) uniform MaterialUniform {
    vec4 MaterialColor;     // xyz=color, w (unused)
    float Shininess;
    float SpecularStrength;
    vec2 _materialPad0;     
};


layout(binding = 0) uniform sampler2D uTexture;
layout(binding = 1) uniform sampler2D uNormalTex;

vec3 CalcDirLight(vec3 normal, vec3 viewDir, vec3 baseColor)
{
    vec3 L = normalize(-uLightDirection.xyz);
    float intensity = uLightSpecularIntensity.w;

    // Diffuse (Lambert)
    float NdotL = max(dot(normal, L), 0.0);
    vec3 diffuse_res = (uLightDiffuse.rgb * intensity) * NdotL * baseColor;

    // Specular (Blinn-Phong)
    vec3 H = normalize(L + viewDir);
    float NdotH = max(dot(normal, H), 0.0);
    float spec = pow(NdotH, max(Shininess, 1.0)) * SpecularStrength;
    spec *= step(0.0, NdotL);
    vec3 specular_res = (uLightSpecularIntensity.xyz * intensity) * spec;

    // Ambient (color * intensity)
    vec3 ambient_res = (uAmbient.xyz * uAmbient.w) * baseColor;

    return ambient_res + diffuse_res + specular_res;
}

void main()
{
    vec3 N = normalize(N_world);
    mat3 TBN = mat3(T_world, B_world, N_world);

    vec3 nTex = texture(uNormalTex, TexCoord).rgb;
    nTex = normalize(nTex * 2.0 - 1.0); // [0,1] -> [-1,1]
    N = normalize(TBN * nTex);          // to world space

    vec3 V = normalize(uCameraPos.xyz - FragPos);

    vec3 baseColor = texture(uTexture, TexCoord).rgb;
    // Optionally multiply by material tint if you want:
    // baseColor *= MaterialColor.rgb;

    vec3 color = CalcDirLight(N, V, baseColor);
    FragColor = vec4(color, 1.0);
}
