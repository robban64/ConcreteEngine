uniform EngineUniform {
    float uTime;
    float uDeltaTime;
    float uRandom;
    float _pad;
    vec2  uInvResolution;
    vec2  uMouse;
};

uniform FrameUniform {
    vec4 uAmbient;
    vec4 uAmbientGround;
    vec4 uFogColor;
    vec4 uFogParams0;
    vec4 uFogParams1;
};

uniform CameraUniform {
    mat4 uViewMat;
    mat4 uProjMat;
    mat4 uProjViewMat;
    vec4 uCameraPos;
    vec4 uCameraUp;
    vec4 uCameraRight;
};

uniform DirLightUniform {
    vec4 uLightDirection;
    vec4 uLightDiffuse;
    vec4 uLightSpecularIntensity;
};

uniform LightUniform {
    ivec4 uLightCounts;
    LightData uLights[MAX_LIGHTS];
};

uniform ShadowUniform {
    mat4 uLightViewProj;
    vec4 uShadowParams0;
    vec4 uShadowParams1;
};

uniform MaterialUniform {
    vec4 Color;
    vec4 SpecularColor;
    vec4 UvTransform;
    
    float Shininess;
    float Roughness;
    float Metallic;
    float AlphaCutoff;
        
    int AlphaMaskToggle;
    int ShadowToggle;
    int _pad22, _pad23;
} uMat;

uniform DrawUniform {
    mat4 uModel;
    mat3 uNormalMat;
};

uniform DrawAnimationUniform {
    mat4 uJointTransforms[64];
};

uniform PostUniform {
    vec4 uGrade;
    vec4 uWhiteBalance;
    vec4 uBloom;
    vec4 uFX;
};

uniform EditorEffectsUniform {
    ivec4 uEffectFlags;
    vec4 uEffectColor1;
};