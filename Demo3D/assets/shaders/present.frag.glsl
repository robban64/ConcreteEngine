#version 420 core

in vec2 TexCoord; 
out vec4 FragColor;

layout(binding = 0) uniform sampler2D uTexture; 
uniform float uGamma = 1.0;

void main() {
    vec3 c = texture(uTexture, TexCoord).rgb;
    if (uGamma > 1.01) c = pow(c, vec3(1.0 / uGamma));
    FragColor = vec4(c, 1.0);
}