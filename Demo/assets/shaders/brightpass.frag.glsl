#version 330 core

in vec2 TexCoord; 
out vec4 FragColor;

uniform sampler2D uSceneTex;      // bind albedo FBO
uniform float uThreshold;      // e.g. 1.0 in linear space
uniform float uSoftKnee;       // e.g. 0.5 (0..1), 0 for hard cut

float luma(vec3 c){ 
    return dot(c, vec3(0.2126,0.7152,0.0722)); 
}

void main(){
    vec3 c = texture(uSceneTex, TexCoord).rgb;
    float br = max(luma(c) - uThreshold, 0.0);
    float knee = uThreshold * uSoftKnee + 1e-5;
    float soft = br > 0.0 ? pow(clamp(br / (br + knee), 0.0, 1.0), 1.0) : 0.0;
    vec3 outc = (br > 0.0 ? c : vec3(0.0)) * max(soft, step(uThreshold, luma(c)));
    FragColor = vec4(outc, 1.0);
}