#version 420 core

in vec2 TexCoord;
out vec4 FragColor;

layout(binding = 0) uniform sampler2D uSceneTex;

@import ubo:EngineUniform
@import ubo:PostUniform

float saturate(float x){ return clamp(x,0.0,1.0); }
vec3  saturate(vec3 v){ return clamp(v,0.0,1.0); }

vec3 linear_to_srgb(vec3 x){ return pow(max(x,0.0), vec3(1.0/2.2)); }
vec3 srgb_to_linear(vec3 x){ return pow(max(x,0.0), vec3(2.2)); }

float luma(vec3 c){ return dot(c, vec3(0.2126,0.7152,0.0722)); }

vec3 apply_saturation(vec3 c, float sat){
    float Y = luma(c);
    return mix(vec3(Y), c, sat);
}
vec3 apply_contrast(vec3 c, float k){
    // pivot in sRGB at 0.5 to avoid crushing lows
    const float pivot = 0.5;
    return (c - pivot) * k + pivot;
}

vec3 apply_white_balance(vec3 c, float warmth, float tint, float strength){
    // tiny RGB gains from warmth (+R, -B) and tint (+G, -M)
    vec3 g = vec3(1.0);
    g.r +=  0.60 * warmth;  g.b -= 0.60 * warmth;
    g.g +=  0.50 * tint;    g.r -= 0.25 * tint; g.b -= 0.25 * tint;
    g = mix(vec3(1.0), g, saturate(strength));
    return c * g;
}

// soft highlight shoulder; symmetric and gentle
vec3 rolloff(vec3 c, float k){
    // k in [0..~0.12]
    float a = max(k, 1e-6);
    return c / (1.0 + a*c);  // Reinhard-like, mild for small a
}

// cheap additive bloom using LOD blur
vec3 sample_bloom(in vec2 uv, float lodBase){
    // 9 taps across a few LODs for soft look
    vec2 px = uInvResolution;
    vec3 sum = vec3(0.0);

    sum += textureLod(uSceneTex, uv, lodBase+0.0).rgb * 0.25;
    sum += textureLod(uSceneTex, uv + vec2(+2,0)*px, lodBase+0.5).rgb * 0.125;
    sum += textureLod(uSceneTex, uv + vec2(-2,0)*px, lodBase+0.5).rgb * 0.125;
    sum += textureLod(uSceneTex, uv + vec2(0,+2)*px, lodBase+0.5).rgb * 0.125;
    sum += textureLod(uSceneTex, uv + vec2(0,-2)*px, lodBase+0.5).rgb * 0.125;
    sum += textureLod(uSceneTex, uv + vec2(+4,+4)*px, lodBase+1.0).rgb * 0.0625;
    sum += textureLod(uSceneTex, uv + vec2(-4,+4)*px, lodBase+1.0).rgb * 0.0625;
    sum += textureLod(uSceneTex, uv + vec2(+4,-4)*px, lodBase+1.0).rgb * 0.0625;
    sum += textureLod(uSceneTex, uv + vec2(-4,-4)*px, lodBase+1.0).rgb * 0.0625;

    return sum;
}

// vignette (luma-preserving)
float vignette(vec2 uv, float strength){
    vec2 d = uv*2.0 - 1.0;
    float r = dot(d,d); // 0 center -> ~2 corner
    float v = 1.0 - strength * smoothstep(0.6, 1.4, r);
    return v;
}

// simple monochrome grain
float hash(vec2 p){
    // low-cost, stable per-frame noise
    p += uTime * 0.5;
    return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453);
}

float hash_px(ivec2 p){
    uint x = uint(p.x), y = uint(p.y);
    uint n = x * 1664525u + y * 1013904223u + 12345u;
    n ^= (n << 13); n ^= (n >> 17); n ^= (n << 5);
    return float(n) * (1.0/4294967296.0); // [0,1)
}

float grain(vec2 uv){
    // lock to pixels
    ivec2 px = ivec2(floor(uv / uInvResolution));
    // tiny frame jitter
    px += ivec2(int(fract(uTime*24.0)*7.0));
    return hash_px(px) * 2.0 - 1.0;
}

// unsharp mask
vec3 sharpen(vec2 uv, vec3 c, float amount, float clampVal){
    vec2 px = uInvResolution;
    vec3 blur =
          texture(uSceneTex, uv + vec2(+1,0)*px).rgb
        + texture(uSceneTex, uv + vec2(-1,0)*px).rgb
        + texture(uSceneTex, uv + vec2(0,+1)*px).rgb
        + texture(uSceneTex, uv + vec2(0,-1)*px).rgb;
    blur = (blur * 0.25);
    vec3 detail = c - blur;
    detail = clamp(detail, -clampVal, clampVal);
    return c + amount * detail;
}

// ---- main ----
void main(){
    vec2 uv = TexCoord;

    // 1) fetch scene (already linear)
    vec3 color = texture(uSceneTex, uv).rgb;

    // 2) exposure (linear)
    float exposure = 1.0 + clamp(uGrade.x, -0.10, 0.10);
    color *= exposure;

    // 3) hop to sRGB for perceptual grading
    vec3 cs = linear_to_srgb(color);

    //   saturation & contrast
    cs = apply_saturation(cs, clamp(uGrade.y, 0.5, 1.5));
    cs = apply_contrast(cs, clamp(uGrade.z, 0.7, 1.3));

    //   warmth & tint
    float warmth = clamp(uGrade.w,        -0.10, 0.10);
    float tint   = clamp(uWhiteBalance.x, -0.10, 0.10);
    float wbStr  = saturate(uWhiteBalance.y);
    cs = apply_white_balance(cs, warmth, tint, wbStr);

    // 4) back to linear
    color = srgb_to_linear(saturate(cs));

    // 5) gentle rolloff (keeps highlights pleasant, doesn’t crush lows)
    color = rolloff(color, clamp(uFX.w, 0.0, 0.12));

    // 6) bloom (threshold in linear, additive)
    float thr = clamp(uBloom.y, 0.4, 0.95);
    float wBright = saturate(max(max(color.r, color.g), color.b) - thr) / max(1.0 - thr, 1e-6);
    vec3 bloom = sample_bloom(uv, max(uBloom.z, 0.0)) * (wBright * clamp(uBloom.x, 0.0, 2.0));
    color += bloom;

    // 7) vignette (applied in linear, mild)
    float vig = vignette(uv, clamp(uFX.x, 0.0, 0.2));
    color *= vig;

    // 8) grain (tiny)
    color += grain(uv) * uFX.y;

    // 9) sharpen (small, clamped)
    color = sharpen(uv, color, clamp(uFX.z, 0.0, 0.12), 0.015);

    // 10) final clamp. Output remains LINEAR (sRGB FBO will encode later).
    FragColor = vec4(saturate(color), 1.0);
}
