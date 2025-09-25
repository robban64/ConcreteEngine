#version 420 core

in vec2 TexCoord;
out vec4 FragColor;

layout(binding = 0) uniform sampler2D uScene;

layout(std140, binding = 5) uniform FramePostProcessUniform
{
    vec4 ColorAdjust;
    vec4 WhiteBalance;
    vec4 Flags;
};

float pow2(float x) {
    return x * x;
}

vec3 applyExposure(vec3 c, float ev)
{
    return c * exp2(ev);
}

vec3 applyContrast(vec3 c, float contrast)
{
    const float pivot = 0.5;
    return (c - pivot) * contrast + pivot;
}

vec3 applySaturation(vec3 c, float sat)
{
    float luma = dot(c, vec3(0.2126, 0.7152, 0.0722));
    return mix(vec3(luma), c, sat);
}

vec3 applyWhiteBalance(vec3 c, float temperature, float tint)
{
    float t = temperature * 0.10;
    vec3 wb = vec3(1.0 + t, 1.0, 1.0 - t);

    float g = tint * 0.10;
    wb *= vec3(1.0 - g, 1.0 + g, 1.0 - g);

    return c * wb;
}

vec3 rollOff(vec3 c, float strength)
{
    float k = mix(0.0, 1.5, clamp(strength, 0.0, 1.0));
    return c / (1.0 + k * c);
}

void main()
{
    vec3 color = texture(uScene, TexCoord).rgb;

    // 1) exposure
    color = applyExposure(color, ColorAdjust.x);

    // 2) white balance (optional)
    if (Flags.y > 0.5)
        color = applyWhiteBalance(color, WhiteBalance.x, WhiteBalance.y);

    // 3) contrast (pivot 0.5)
    color = applyContrast(color, ColorAdjust.y);

    // 4) saturation
    color = applySaturation(color, ColorAdjust.z);

    // 5) soft highlight roll-off
    color = rollOff(color, Flags.z);

    // 6) output encode (only if default framebuffer is NOT sRGB)
    if (Flags.x > 0.5)
        color = pow(color, vec3(1.0 / max(ColorAdjust.w, 1e-6)));

    FragColor = vec4(clamp(color, 0.0, 1.0), 1.0);
}
