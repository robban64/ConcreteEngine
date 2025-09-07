#version 420 core

in vec3 FragPos;
in vec2 TexCoord;
in vec3 Normal;

out vec4 FragColor;

// FrameGlobalUniformGpuData
layout(std140, binding = 0) uniform FrameGlobalUniform {
    vec4 uAmbient;   // xyz=color, w=intensity
    vec4 uFogColor;  // xyz=color, w=density
    vec4 uFogDetail; // x=near, y=far, z=type, w=0
};

// CameraUniformGpuData (unused here but available)
layout(std140, binding = 1) uniform CameraUniform {
    mat4 uViewMat;
    mat4 uProjMat;
    mat4 uProjViewMat;
    vec4 uCameraPos;
};

// DirLightUniformGpuData
layout(std140, binding = 2) uniform DirLightUniform {
    vec4 uLightDirection;            // xyz, w unused
    vec4 uLightDiffuse;              // rgb, w unused
    vec4 uLightSpecularIntensity;    // xyz=specular, w=intensity
};

// MaterialUniformGpuData (unused for terrain lighting here)
layout(std140, binding = 3) uniform MaterialUniform {
    vec4 MaterialColor;
    float Shininess;
    float SpecularStrength;
    vec2 _materialPad0;
};

layout(binding = 0) uniform sampler2D uTexture;

// Tiling factor
uniform float uTexCoordRepeat;

vec3 CalcDirLight(vec3 normal, vec3 texColor)
{
    vec3 L = normalize(-uLightDirection.xyz);
    float NdotL = max(dot(normal, L), 0.0);
    float intensity = uLightSpecularIntensity.w;

    vec3 ambient_res = (uAmbient.xyz * uAmbient.w) * texColor;
    vec3 diffuse_res = (uLightDiffuse.rgb * intensity) * NdotL * texColor;

    return ambient_res + diffuse_res;
}

void main()
{
    vec3 n = normalize(Normal);
    vec2 tiled = TexCoord * uTexCoordRepeat;
    vec3 texColor = texture(uTexture, tiled).rgb;

    vec3 color = CalcDirLight(n, texColor);
    FragColor = vec4(color, 1.0);
}
