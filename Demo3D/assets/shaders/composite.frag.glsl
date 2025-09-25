#version 420 core

in vec2 TexCoord;
out vec4 FragColor;

layout(binding = 0) uniform sampler2D uSceneTex;
//layout(binding = 1) uniform sampler2D uLightTex;

uniform float uLightScale = 1.0;
uniform float uSceneScale = 1.0;

void main() {
    vec3 scene = texture(uSceneTex, TexCoord).rgb * uSceneScale;
    //vec3 light = texture(uLightTex, TexCoord).rgb * uLightScale;
    //vec3 color = scene + light;
    //FragColor = vec4(clamp(color, 0.0, 1.0), 1.0);
    FragColor = vec4(scene, 1.0);
}
