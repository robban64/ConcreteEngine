#version 330 core

struct DirLight
{
    vec3 direction;
    vec3 diffuse;
    vec3 specular;
    float intensity;
};

in vec3 FragPos;
in vec2 TexCoord;
in vec3 Normal;

out vec4 FragColor;

uniform sampler2D uTexture;

uniform vec3 uAmbient;
uniform DirLight uLight;

uniform float uTexCoordRepeat;

vec3 CalcDirLight(vec3 normal, vec3 texColor);

void main()
{
	vec3 norm = normalize(Normal);

	vec2 tiledCoords = TexCoord * uTexCoordRepeat;
    vec3 texColor = vec3(texture(uTexture, tiledCoords));
    vec3 result = CalcDirLight(norm, texColor);
	FragColor = vec4(result, 1.0);
}

vec3 CalcDirLight(vec3 normal, vec3 texColor)
{
	vec3 lightDir = normalize(-uLight.direction);
	float diff = max(dot(normal, lightDir), 0.0);

	vec3 ambient_res = uAmbient * texColor;
	vec3 diffuse_res = (uLight.diffuse * uLight.intensity) * diff * texColor;

	return ambient_res + diffuse_res;   
}