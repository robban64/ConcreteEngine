#version 420 core

in vec3 FragPos;
in vec2 TexCoord;
in vec3 Normal;

out vec4 FragColor;

#include(Frame)
#include(Camera)
#include(DirLight)
#include(Material)

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
