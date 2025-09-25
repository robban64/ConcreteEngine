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

    // Step 1: core grading
    color = applyExposure(color, ColorAdjust.x);
    if (Flags.y > 0.5)
        color = applyWhiteBalance(color, WhiteBalance.x, WhiteBalance.y);
    color = applyContrast(color, ColorAdjust.y);
    color = applySaturation(color, ColorAdjust.z);

    // Step 2: mip-bloom
    float threshold = BloomParams.x;
    float softKnee  = clamp(BloomParams.y, 0.0, 1.0);
    float intensity = max(BloomParams.z, 0.0);
    float lodBias   = BloomParams.w;

    float knee      = threshold * softKnee + 1e-5;
    float invKnee4  = 0.25 / knee;
    float curve0    = threshold - knee;
    float curve1    = 2.0 * knee;

    vec3 bloom = vec3(0.0);
    for (int i = 0; i < 4; ++i)
    {
        float lod = float(i + 1) + lodBias;
        vec3 s = textureLod(uScene, TexCoord, lod).rgb;
        vec3 x = max(s - vec3(curve0), 0.0);
        vec3 soft = min(x * invKnee4, (s - vec3(curve0)) / vec3(curve1) + 0.5);
        vec3 bright = max(x, soft);
        float w = (i == 0) ? BloomLods.x :
                  (i == 1) ? BloomLods.y :
                  (i == 2) ? BloomLods.z : BloomLods.w;
        bloom += bright * w;
    }
    color += bloom * intensity;

    // Step 3a: 3D LUT (linear->linear); apply after bloom

    // Update once LUT is working
    //float lutIntensity = clamp(LutParams.x, 0.0, 1.0);
    float lutIntensity = clamp(0.0, 0.0, 1.0);
    if (lutIntensity > 0.0)
    {
        float invSize = LutParams.y; // = 1.0 / LUT_SIZE (e.g., 1/32)
        vec3 uvw = clamp(color, 0.0, 1.0);
        // sample at texel centers to avoid edge bias
        uvw = uvw * (1.0 - invSize) + 0.5 * invSize;
        vec3 lutColor = texture(uLUT, uvw).rgb;
        color = mix(color, lutColor, lutIntensity);
    }

    // Step 3b: (vignette + film grain)
    // Vignette
    float vigStrength = VignetteParams.x;
    float vigRadius   = VignetteParams.y;
    float vigSoft     = VignetteParams.z;
    if (vigStrength > 0.0)
    {
        vec2 p = TexCoord * 2.0 - 1.0; // [-1,1]
        float r = length(p);
        float edge = smoothstep(vigRadius, vigRadius + max(vigSoft, 1e-5), length(TexCoord*2.0-1.0));
        color *= mix(1.0, 1.0 - edge, clamp(vigStrength, 0.0, 2.0));
    }

    // Grain (temporal, filmic ~ very low amplitude)
    float grainAmt = 0; //GrainParams.x;
    if (grainAmt > 0.0)
    {
        float t = GrainParams.y;
        // hash noise
        float n = fract(sin(dot(TexCoord * vec2(1280.0, 720.0) + t, vec2(12.9898,78.233))) * 43758.5453);
        n = n * 2.0 - 1.0; // [-1,1]
        color += n * grainAmt;
    }

    // (Optional) very subtle chromatic aberration (disabled by default)
    float cab = ChromAbParams.x;
    if (cab > 0.0)
    {
        vec2 dir = (TexCoord - 0.5) * 2.0;
        vec2 off = dir * cab;
        float r = texture(uScene, TexCoord + off).r;
        float b = texture(uScene, TexCoord - off).b;
        color = vec3(r, color.g, b);
    }

    // highlight roll-off + output
    color = rollOff(color, Flags.z);
    if (Flags.x > 0.5)
        color = pow(color, vec3(1.0 / max(ColorAdjust.w, 1e-6)));

    FragColor = vec4(clamp(color, 0.0, 1.0), 1.0);
}
