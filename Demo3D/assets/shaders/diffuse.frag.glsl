#version 330 core

struct DirLight {
    vec3 direction;
    vec3 diffuse;
    vec3 specular;
    float intensity;
};

struct Material {
    float shininess;
    float specularStrength;
};

in vec3 FragPos;
in vec2 TexCoord;

in vec3 N_world;
in vec3 T_world;
in vec3 B_world;

out vec4 FragColor;

uniform vec3 uCameraPos;
uniform vec3 uAmbient;
uniform DirLight uLight;
uniform Material uMaterial;

uniform sampler2D uTexture;
uniform sampler2D uNormalTex;

vec3 CalcDirLight(vec3 normal, vec3 viewDir, vec3 baseColor);

void main()
{
    vec3 N = normalize(N_world);
    mat3 TBN = mat3(T_world, B_world, N_world);
    vec3 nTex = texture(uNormalTex, TexCoord).rgb;
    nTex = normalize(nTex * 2.0 - 1.0);   // [0,1] -> [-1,1]
    N = normalize(TBN * nTex);            // to world space

    vec3 V = normalize(uCameraPos - FragPos);
    vec3 baseColor = texture(uTexture, TexCoord).rgb;

    vec3 color = CalcDirLight(N, V, baseColor);
    FragColor = vec4(color, 1.0);
}

vec3 CalcDirLight(vec3 normal, vec3 viewDir, vec3 baseColor)
{
    vec3 L = normalize(-uLight.direction);

    // Diffuse (Lambert)
    float NdotL = max(dot(normal, L), 0.0);
    vec3 diffuse_res = (uLight.diffuse * uLight.intensity) * NdotL * baseColor;

    // Specular (Blinn-Phong)
    vec3 H = normalize(L + viewDir);
    float NdotH = max(dot(normal, H), 0.0);
    float spec = pow(NdotH, max(uMaterial.shininess, 1.0)) * uMaterial.specularStrength;
    // zero spec if the surface faces away from light
    spec *= step(0.0, NdotL);

    vec3 specular_res = (uLight.specular * uLight.intensity) * spec;

    // Ambient (separate, so you don't need >0.5 to see anything)
    vec3 ambient_res = uAmbient * baseColor;

    return ambient_res + diffuse_res + specular_res;

}


