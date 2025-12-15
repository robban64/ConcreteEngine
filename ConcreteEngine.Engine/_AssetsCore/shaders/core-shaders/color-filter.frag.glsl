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

// Grading functions
vec3 apply_saturation(vec3 c, float sat){
    float Y = luma(c);
    return mix(vec3(Y), c, sat);
}
vec3 apply_contrast(vec3 c, float k){
    const float pivot = 0.5;
    return (c - pivot) * k + pivot;
}

vec3 apply_white_balance(vec3 c, float warmth, float tint, float strength){
    vec3 g = vec3(1.0);
    g.r +=  0.60 * warmth;  g.b -= 0.60 * warmth;
    g.g +=  0.50 * tint;    g.r -= 0.25 * tint; g.b -= 0.25 * tint;
    g = mix(vec3(1.0), g, saturate(strength));
    return c * g;
}

vec3 rolloff(vec3 c, float k){
    float a = max(k, 1e-6);
    return c / (1.0 + a*c); 
}

vec3 sample_bloom(in vec2 uv, float lodBase){
    // 9-tap Tent Filter
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

float vignette(vec2 uv, float strength){
    vec2 d = uv*2.0 - 1.0;
    float r = dot(d,d); 
    return 1.0 - strength * smoothstep(0.5, 1.5, r);
}

// simple monochrome grain
float hash(vec2 p) { 
    return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453); 
}

float grain(vec2 uv){
    // Animated noise
    float n = hash(uv + vec2(uTime * 10.0, 0.0));
    return n * 2.0 - 1.0; 
}

vec3 sharpen(vec2 uv, vec3 c, float amount){
    vec2 px = uInvResolution;
    vec3 blur = texture(uSceneTex, uv + vec2(1,0)*px).rgb
              + texture(uSceneTex, uv + vec2(-1,0)*px).rgb
              + texture(uSceneTex, uv + vec2(0,1)*px).rgb
              + texture(uSceneTex, uv + vec2(0,-1)*px).rgb;
    blur *= 0.25;
    // Simple Unsharp Mask
    return c + (c - blur) * amount;
}


void main(){
    vec2 uv = TexCoord;
    vec3 color = texture(uSceneTex, uv).rgb;

    color *= max(uGrade.x, 0.0);

    vec3 cs = linear_to_srgb(color); 

    cs = apply_saturation(cs, uGrade.y);
    cs = apply_contrast(cs, uGrade.z);
    
    // Warmth/Tint
    cs = apply_white_balance(cs, uGrade.w, uWhiteBalance.x, saturate(uWhiteBalance.y));

    color = srgb_to_linear(max(cs, 0.0));

    // Bloom Pseudo-HDR values
    float thr = uBloom.y; 
    vec3 bright = max(color - vec3(thr), 0.0);
    
    color += sample_bloom(uv, uBloom.z) * (bright * uBloom.x);

    // FX
    color *= vignette(uv, uFX.x);
    
    // Grain 
    color += grain(uv) * (uFX.y * 0.04);
    color = sharpen(uv, color, uFX.z);

    FragColor = vec4(saturate(color), 1.0);
}
