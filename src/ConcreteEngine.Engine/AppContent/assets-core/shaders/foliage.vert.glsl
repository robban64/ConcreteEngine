#version 420 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in vec4 aTangent;

layout (location = 4) in mediump vec4 aInstancePosition;
layout (location = 5) in vec4 aInstanceColor;

out VS_OUT {
    vec3 FragPos;
    vec2 TexCoord;
    vec3 N_world;
    vec4 FoliageColor;
} vs_out;

@import ubo:EngineUniform
@import ubo:CameraUniform

void main() {

    float worldScale = aInstancePosition.w;
    float dist = distance(uCameraPos.xyz, aInstancePosition.xyz);
    float dropThreshold = mix(50.0, 100.0, worldScale);
    if (dist > dropThreshold) {
        const float fadeZone = 10.0;
        float scaleModifier = 1.0 - clamp((dist - dropThreshold) / fadeZone, 0.0, 1.0);
        
        worldScale *= scaleModifier;
    }

    vec3 pos = (aPos * worldScale) + aInstancePosition.xyz;
    if(aTexCoord.y <= 0.1) {
        const float windSpeed = 0.05;
        pos.x += sin(uTime + aInstancePosition.x) * windSpeed;
        pos.z += sin(uTime + aInstancePosition.z) * windSpeed;
    }
    
    vs_out.FragPos = pos;
    vs_out.TexCoord = aTexCoord;
    vs_out.FoliageColor = aInstanceColor;
    vs_out.N_world = aNormal;

    gl_Position = uProjViewMat * vec4(pos, 1.0);
}
