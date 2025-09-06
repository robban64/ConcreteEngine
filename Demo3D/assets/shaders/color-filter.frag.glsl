#version 330 core

in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D uScene;

void main()
{
    vec3 tint = vec3(1.0, 0.8, 0.8);
    vec3 color = texture(uScene, TexCoord).rgb;

    float gray = dot(color, vec3(0.299, 0.587, 0.114));
    vec3 filtered = mix(color, vec3(gray), 0.5); 
    filtered *= tint;

    FragColor = vec4(filtered, 1.0);
}