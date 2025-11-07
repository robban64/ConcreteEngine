#version 420 core

in vec2 TexCoord; 
out vec4 FragColor;

layout(binding = 0) uniform sampler2D uTexture; 

//uniform float uGamma = 2.2;

void main() {
    float uGamma = 2.2;
    vec3 c = texture(uTexture, TexCoord).rgb; // linear
    c = pow(c, vec3(1.0 / uGamma)); // linear -> gamma
    FragColor = vec4(c, 1.0);

}