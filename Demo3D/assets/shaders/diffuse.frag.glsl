#version 330 core

struct DirLight
{
    vec3 direction;
    vec3 diffuse;
    vec3 specular;
    float intensity;
};

in vec3 FragPos;
in vec3 Normal;
in vec2 TexCoord;

out vec4 FragColor;

uniform vec3 uAmbient;
uniform DirLight uLight;
uniform sampler2D uTexture;

vec3 CalcDirLight(vec3 normal);

void main()
{
	vec3 norm = normalize(Normal);
	vec3 result = CalcDirLight(norm);

	FragColor = vec4(result, 1.0);
}

vec3 CalcDirLight(vec3 normal)
{
	vec3 lightDir = normalize(-uLight.direction);
	float diff = max(dot(normal, lightDir), 0.0);

    vec3 texColor = vec3(texture(uTexture, TexCoord));

	vec3 ambient_res = uAmbient * texColor;
	vec3 diffuse_res = (uLight.diffuse * uLight.intensity) * diff * texColor;

	return ambient_res + diffuse_res;   
}

	//vec3 viewDir = normalize(cameraPosition - FragPos);
	//vec3 halfwayDir = normalize(lightDir + viewDir);
	//float spec = pow(max(dot(normal, halfwayDir), 0.0), 32.0);
	//vec3 specular_res = uLight.specular * spec * material.shininess;
 // + specular_res;

