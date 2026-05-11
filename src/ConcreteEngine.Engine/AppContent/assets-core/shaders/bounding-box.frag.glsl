#version 420 core

in vec2 TexCoord;
in vec3 FragPos;

out vec4 FragColor;

@import ubo:EngineUniform
@import ubo:EditorEffectsUniform

const float EDGE_PX = 1.5;
const float CORNER_PX = 14.0;
const float EDGE_ALPHA = 0.90;
const float FILL_ALPHA = 0.40;

void main()
{
    vec3 col = uEffectColor1.rgb;

    vec2 fw = fwidth(TexCoord);

    vec2 distPx = min(TexCoord, 1.0 - TexCoord) / fw;

    float edgeDist = min(distPx.x, distPx.y);
    float edge = 1.0 - smoothstep(EDGE_PX - 0.5, EDGE_PX + 0.5, edgeDist);

    vec2 cornerDistPx = min(TexCoord, 1.0 - TexCoord) / fw;
    bool inCorner = cornerDistPx.x < CORNER_PX && cornerDistPx.y < CORNER_PX;
    float corner = edge * float(inCorner);

    float alpha = max(FILL_ALPHA, edge * EDGE_ALPHA);

    if (alpha < 0.01) discard;

    float brightness = 1.0 + 0.35 * corner;

    FragColor = vec4(col * brightness, alpha);
}