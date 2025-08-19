#version 330 core

in vec2 TexCoord; 
out vec4 FragColor;

uniform sampler2D uSceneTex;     // albedo/scene (at screen scale)
uniform sampler2D uBloomTex;     // blurred bright (upsampled or sampled at half-res with same UV)

uniform float uBloomStrength; // e.g. 0.6
uniform float uVignetteRadius;// e.g. 0.75 (radius from center)
uniform float uVignetteSoft;  // e.g. 0.45 (softness)
uniform float uVignetteGain;  // e.g. 1.0..1.2 (extra darkening)

void main(){
    vec3 scene = texture(uSceneTex, TexCoord).rgb;
    vec3 bloom = texture(uBloomTex, TexCoord).rgb;
    vec3 color = scene + uBloomStrength * bloom;

    // Vignette: radial darken from center
    vec2 uv = TexCoord * 2.0 - 1.0;             // [-1,1]
    float d = dot(uv, uv);                  // squared radius
    float r = uVignetteRadius*uVignetteRadius;
    float s = max(uVignetteSoft*0.5, 1e-5);
    float vig = 1.0 - smoothstep(r, r + s, d);
    vig = pow(vig, uVignetteGain);
    color *= vig;

    FragColor = vec4(color, 1.0);
}