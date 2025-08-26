#version 330 core
in vec2 TexCoord;
out vec4 FragColor;
uniform sampler2D uSceneTex;
const float threshold = 0.60;  // LDR-friendly
const float softKnee  = 0.50;
const float strength  = 0.50;  // how much to add

void main(){
    vec3 c = texture(uSceneTex, TexCoord).rgb;
    float luma = dot(c, vec3(0.2126,0.7152,0.0722));
    float knee = threshold * softKnee + 1e-5;
    float t    = max(luma - threshold + knee, 0.0);
    float soft = t*t / (4.0*knee + 1e-5);
    float mask = clamp(max(luma - threshold, 0.0) + soft, 0.0, 1.0);
    FragColor = vec4(c * mask * strength, 1.0);
}