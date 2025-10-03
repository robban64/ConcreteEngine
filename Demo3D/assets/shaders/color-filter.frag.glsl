#version 420 core

in vec2 TexCoord;
out vec4 FragColor;

layout(binding = 0) uniform sampler2D uScene;
layout(binding = 1) uniform sampler3D uLUT;

#include(PostProcess)

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

// Highlight rolloff
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

vec3 chromaticAberration(vec2 uv, vec3 c)
{
    float amount = ChromAbParams.x;
    if (amount <= 0.0) return c;
    vec2 dir = (uv - 0.5) * 2.0;
    vec2 off = dir * amount;
    float r = texture(uScene, uv + off).r;
    float b = texture(uScene, uv - off).b;
    return vec3(r, c.g, b);
}

vec3 vignette(vec2 uv, vec3 c)
{
    float inner = clamp(VignetteParams.x, 0.0, 1.0);
    float outer = max(VignetteParams.y, inner + 1e-3);
    float amt = clamp(VignetteParams.z, 0.0, 1.0);

    float d = distance(uv, vec2(0.5));
    float v = smoothstep(inner, outer, d);
    return c * mix(1.0, 1.0 - v, amt);
}

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

vec3 addGrain(vec2 uv, vec3 c)
{
    float inten = clamp(GrainParams.x, 0.0, 0.1);
    if (inten <= 0.0) return c;

    // Tie noise to pixel grid to avoid shimmer; modulate by time
    vec2 res = vec2(textureSize(uScene, 0));
    vec2 ip = floor(uv * res);
    float n = hash(ip + GrainParams.yy);

    // Grain in luma space, re-injected to RGB
    float l = dot(c, vec3(0.2126, 0.7152, 0.0722));
    float g = (n - 0.5) * 2.0 * inten;
    float nl = clamp(l + g, 0.0, 1.0);
    return c + (nl - l);
}

vec3 hueToRgb(float deg)
{
    float h = fract(deg / 360.0);
    vec3 k = vec3(1.0, 2.0 / 3.0, 1.0 / 3.0);
    return clamp(abs(mod(h + k, 1.0) * 6.0 - 3.0) - 1.0, 0.0, 1.0);
}

vec3 applySplitToning(vec3 c)
{
    float l = dot(c, vec3(0.2126, 0.7152, 0.0722));
    // Shadow tint
    vec3 sc = hueToRgb(ToneShadows.x) * ToneShadows.y;
    sc = mix(vec3(0.0), sc, ToneShadows.w);
    sc += vec3(ToneShadows.z); // luminance bias

    // Highlight tint
    vec3 hc = hueToRgb(ToneHighlights.x) * ToneHighlights.y;
    hc = mix(vec3(0.0), hc, ToneHighlights.w);
    hc += vec3(ToneHighlights.z);

    float sh = 1.0 - smoothstep(0.25, 0.5, l);
    float hi = smoothstep(0.5, 0.75, l);
    vec3 tint = sc * sh + hc * hi;
    return clamp(c + tint, 0.0, 1.0);
}

vec3 unsharp(vec2 uv, vec3 c)
{
    float amount = clamp(SharpenParams.x, 0.0, 0.6);
    if (amount <= 0.0) return c;

    float rad = clamp(SharpenParams.y, 1.0, 3.0);
    float thr = clamp(SharpenParams.z, 0.0, 0.25);

    vec2 texel = 1.0 / vec2(textureSize(uScene, 0));
    vec2 r = texel * rad;

    // 9-tap Gaussian-ish blur
    vec3 b = vec3(0.0);
    b += texture(uScene, uv + vec2(-r.x, -r.y)).rgb;
    b += texture(uScene, uv + vec2(0.0, -r.y)).rgb * 2.0;
    b += texture(uScene, uv + vec2(r.x, -r.y)).rgb;

    b += texture(uScene, uv + vec2(-r.x, 0.0)).rgb * 2.0;
    b += texture(uScene, uv).rgb * 4.0;
    b += texture(uScene, uv + vec2(r.x, 0.0)).rgb * 2.0;

    b += texture(uScene, uv + vec2(-r.x, r.y)).rgb;
    b += texture(uScene, uv + vec2(0.0, r.y)).rgb * 2.0;
    b += texture(uScene, uv + vec2(r.x, r.y)).rgb;

    b *= 1.0 / 16.0;

    vec3 diff = c - b;
    // Threshold in luma to avoid noise amplification
    float dl = abs(dot(diff, vec3(0.2126, 0.7152, 0.0722)));
    float m = smoothstep(thr, thr + 0.1, dl);
    return clamp(c + diff * (amount * m), 0.0, 1.0);
}

void main()
{
    vec3 color = texture(uScene, TexCoord).rgb;

    // grading
    color = applyExposure(color, ColorAdjust.x);
    color = applyWhiteBalance(color, WhiteBalance.x, WhiteBalance.y);
    color = applyVibrance(color, WhiteBalance.z);
    color = applySaturation(color, ColorAdjust.z);
    color = applyContrast(color, ColorAdjust.y);

    // tweaks
    color = applySplitToning(color);
    color += bloomSample(TexCoord);
    color = vignette(TexCoord, color);
    color = unsharp(TexCoord, color);
    color = addGrain(TexCoord, color);
    color = chromaticAberration(TexCoord, color);

    // final
    color = rollOff(color, Flags.z);
    if (Flags.x > 0.5)
        color = pow(safe_saturate(color), vec3(1.0 / max(ColorAdjust.w, 1e-6)));

    FragColor = vec4(safe_saturate(color), 1.0);
}
