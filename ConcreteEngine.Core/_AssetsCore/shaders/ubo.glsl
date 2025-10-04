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
};

uniform DirLightUniform {
    vec4 uLightDirection;
    vec4 uLightDiffuse;
    vec4 uLightSpecularIntensity;
};

uniform LightUniform {
    ivec4 uLightCounts; // x = lightCount (0..MAX_LIGHTS), yzw reserved
    LightData uLights[MAX_LIGHTS];
};

uniform ShadowUniform {
    mat4 uLightViewProj;
    vec4 uShadowParams0; // x=1/texW, y=1/texH, z=constBias, w=slopeBias
    vec4 uShadowParams1; // x=strength, y=pcfRadius, z,w reserved
};

uniform MaterialUniform {
    vec4 uMatColor;
    vec4 uMatParams0;
    vec4 uMatParams1;
};

uniform DrawUniform {
    mat4 uModel;
    // normal matrix as vec4 (xyz used)
    vec4 uNormalCol0;
    vec4 uNormalCol1;
    vec4 uNormalCol2;
};

uniform PostUniform {
    vec4 ColorAdjust;
    vec4 WhiteBalance;
    vec4 Flags;
    vec4 BloomParams;
    vec4 BloomLods;
    vec4 LutParams;

    vec4 VignetteParams;
    vec4 GrainParams;
    vec4 ChromAbParams;

    vec4 ToneShadows;
    vec4 ToneHighlights;
    vec4 SharpenParams;
};
