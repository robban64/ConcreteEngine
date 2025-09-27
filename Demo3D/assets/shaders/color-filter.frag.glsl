#version 420 core

in vec2 TexCoord;
out vec4 FragColor;

layout(binding = 0) uniform sampler2D uScene;
layout(binding = 1) uniform sampler3D uLUT;

layout(std140, binding = 5) uniform FramePostProcessUniform
{
    vec4 ColorAdjust;
    vec4 WhiteBalance;
    vec4 Flags;
    vec4 BloomParams;
    vec4 BloomLods;
    vec4 LutParams;
    vec4 VignetteParams;
    vec4 GrainParams;
    vec4 ChromAbParams;
};

float luma709(vec3 c) {
    return dot(c, vec3(0.2126, 0.7152, 0.0722));
}
vec3 safe_saturate(vec3 c) {
    return clamp(c, 0.0, 1.0);
}

// Exposure in EV (stops)
vec3 applyExposure(vec3 c, float ev) {
    return c * exp2(ev);
}

// Contrast around pivot 0.5 (LDR)
vec3 applyContrast(vec3 c, float contrast)
{
    return (c - 0.5) * contrast + 0.5;
}

// Saturation with luminance pivot
vec3 applySaturation(vec3 c, float sat)
{
    float l = luma709(c);
    return mix(vec3(l), c, sat);
}

// Vibrance: boosts low-sat regions more than high-sat
vec3 applyVibrance(vec3 c, float vib)
{
    float l = luma709(c);
    float sat = clamp(length(c - vec3(l)), 0.0, 1.0);
    float k = vib * (1.0 - sat);
    return mix(vec3(l), c, 1.0 + k);
}

vec3 applyWhiteBalanceSimple(vec3 c, float temperature, float tint)
{
    float t = clamp(temperature, -1.0, 1.0);
    float g = clamp(tint, -1.0, 1.0);
    vec3 gains = vec3(1.0 + 0.10 * t, 1.0 + 0.06 * g, 1.0 - 0.10 * t);
    c *= gains;
    // preserve luminance a bit
    float l = luma709(c);
    return mix(vec3(l), c, 0.85);
}

vec3 applyWhiteBalance(vec3 c, float temperature, float tint)
{
    // RGB -> LMS
    const mat3 M = mat3(
            0.3811, 0.5783, 0.0406,
            0.1967, 0.7244, 0.0787,
            0.0241, 0.1288, 0.8444
        );
    const mat3 Minv = mat3(
            4.4679, -3.5873, 0.1193,
            -1.2186, 2.3809, -0.1624,
            0.0497, -0.2439, 1.2045
        );

    vec3 lms = M * c;

    float t = clamp(temperature, -1.0, 1.0);
    float g = clamp(tint, -1.0, 1.0);

    vec3 scale = vec3(1.0 + 0.15 * t, 1.0 + 0.10 * g, 1.0 - 0.15 * t);
    lms *= scale;

    return Minv * lms;
}

// Highlight rolloff: compresses bright values smoothly
vec3 rollOff(vec3 c, float strength)
{
    strength = max(strength, 0.0);
    // Rational curve: c' = c / (1 + strength * c)
    return c / (1.0 + strength * c);
}

// Bloom from scene mip chain
vec3 bloomSample(vec2 uv)
{
    float threshold = BloomParams.x;
    float softKnee = clamp(BloomParams.y, 0.0, 1.0);
    float extra = max(BloomParams.z, 0.0);
    float lodBias = BloomParams.w;

    // prefilter curve (soft knee)
    float knee = threshold * softKnee + 1e-6;
    float invK4 = 0.25 / knee;
    float curve0 = threshold - knee;
    float curve1 = 2.0 * knee;

    vec3 sum = vec3(0.0);
    for (int i = 0; i < 4; ++i)
    {
        float lod = float(i + 1) + lodBias;
        vec3 s = textureLod(uScene, uv, lod).rgb;
        float br = max(max(s.r, s.g), s.b);

        // soft knee weighting
        float wk = max(br - curve0, 0.0);
        wk = wk * wk * invK4;
        wk = min(wk, br - threshold);
        wk = max(wk, 0.0);

        sum += s * wk * BloomLods[i];
    }

    float intensity = max(Flags.w, 0.0) * (1.0 + extra);
    return sum * intensity;
}

// Screen-space chromatic aberratio
vec3 chromaticAberration(vec2 uv, vec3 c, float amount)
{
    if (amount <= 0.0) return c;
    vec2 dir = (uv - 0.5) * 2.0;
    vec2 off = dir * amount;

    float r = texture(uScene, uv + off).r;
    float b = texture(uScene, uv - off).b;
    return vec3(r, c.g, b);
}

// Vignette (WhiteBalance.w = amount 0..1)
vec3 vignette(vec2 uv, vec3 c, float amount)
{
    if (amount <= 0.0) return c;
    float d = distance(uv, vec2(0.5));

    // Smooth falloff; 0.75 for LDR
    float v = smoothstep(0.75, 0.95, d);
    return c * mix(1.0, 1.0 - v, amount);
}

// ---- Main ------------------------------------------------------------------

void main()
{
    vec3 color = texture(uScene, TexCoord).rgb;

    // grading order
    color = applyExposure(color, ColorAdjust.x);
    color = applyWhiteBalance(color, WhiteBalance.x, WhiteBalance.y);
    color = applyVibrance(color, WhiteBalance.z);
    color = applySaturation(color, ColorAdjust.z);
    color = applyContrast(color, ColorAdjust.y);

    color += bloomSample(TexCoord);

    color = chromaticAberration(TexCoord, color, Flags.y);

    color = vignette(TexCoord, color, WhiteBalance.w);

    color = rollOff(color, Flags.z);

    // output gamma (sRGB display).
    if (Flags.x > 0.5)
        color = pow(safe_saturate(color), vec3(1.0 / max(ColorAdjust.w, 1e-6)));

    FragColor = vec4(safe_saturate(color), 1.0);
}
