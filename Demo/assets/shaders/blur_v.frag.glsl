#version 330 core

in vec2 TexCoord; 
out vec4 FragColor;

uniform sampler2D uTexture;
uniform vec2 uTexelSize;

void main(){
    float w0=0.227027, w1=0.316216, w2=0.070270;
    vec2 o = vec2(0.0, uTexelSize.y);
    vec3 c = texture(uTexture, TexCoord).rgb * w0
           + texture(uTexture, TexCoord + 1.384615*o).rgb * w1
           + texture(uTexture, TexCoord - 1.384615*o).rgb * w1
           + texture(uTexture, TexCoord + 3.230769*o).rgb * w2
           + texture(uTexture, TexCoord - 3.230769*o).rgb * w2;

    FragColor = vec4(c,1.0);
}