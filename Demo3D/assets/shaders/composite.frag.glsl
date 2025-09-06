#version 330 core

in vec2 TexCoord; 
out vec4 FragColor;

uniform sampler2D uSceneTex;
uniform sampler2D uLightTex; 

uniform float uLightScale = 1.0;
uniform float uSceneScale = 1.0;

void main() {
    vec3 scene = texture(uSceneTex, TexCoord).rgb * uSceneScale;
    vec3 light = texture(uLightTex, TexCoord).rgb * uLightScale;
    vec3 color = scene + light;
    FragColor = vec4(clamp(color, 0.0, 1.0), 1.0);
}
