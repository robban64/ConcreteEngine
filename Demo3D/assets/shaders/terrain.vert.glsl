#version 420 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;

out vec3 FragPos;
out vec2 TexCoord;
out vec3 Normal;

// FrameGlobalUniformGpuData
layout(std140, binding = 0) uniform FrameGlobalUniform {
    vec4 uAmbient;
    vec4 uFogColor;
    vec4 uFogDetail;
};

// CameraUniformGpuData
layout(std140, binding = 1) uniform CameraUniform {
    mat4 uViewMat;
    mat4 uProjMat;
    mat4 uProjViewMat;
    vec4 uCameraPos;
};

// DirLightUniformGpuData
layout(std140, binding = 2) uniform DirLightUniform {
    vec4 uLightDirection;
    vec4 uLightDiffuse;
    vec4 uLightSpecularIntensity;
};

// MaterialUniformGpuData (not used here, present for completeness)
layout(std140, binding = 3) uniform MaterialUniform {
    vec4 MaterialColor;
    float Shininess;
    float SpecularStrength;
    vec2 _materialPad0;
};

// DrawUniformGpuData
layout(std140, binding = 4) uniform DrawUniform {
    mat4 uModel;
    vec4 uNormalCol0;
    vec4 uNormalCol1;
    vec4 uNormalCol2;
};

void main()
{
    vec4 worldPosition = uModel * vec4(aPos, 1.0);
    FragPos = worldPosition.xyz;
    TexCoord = aTexCoord;

    mat3 normalMat = mat3(uNormalCol0.xyz, uNormalCol1.xyz, uNormalCol2.xyz);
    Normal = normalize(normalMat * aNormal);

    gl_Position = uProjViewMat * worldPosition;
}
