#version 330 core
in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D uSceneTex;
uniform sampler2D uLightTex;
uniform vec2      uTexelSize; // (lightW, lightH)


vec2 snapToTexel(vec2 uv, vec2 texSize)
{
    // Floor to the nearest texel center
    vec2 st = uv * texSize;
    st = (floor(st) + 0.5) / texSize;
    return st;
}


void main()
{
    vec4 scene = texture(uSceneTex, TexCoord);

    vec2 uvLight = snapToTexel(TexCoord, uTexelSize);
    vec3 light = texture(uLightTex, uvLight).rgb;

    // Multiply: scene * (ambient + lights)
    vec3 lit = scene.rgb * light;

    FragColor = vec4(lit, scene.a);
}