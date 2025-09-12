#version 420 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord;
layout (location = 2) in vec3 aNormal;

out vec3 FragPos;
out vec2 TexCoord;
out vec3 Normal;

#include(Frame)
#include(Camera)
#include(DrawObject)

void main()
{
    vec4 worldPosition = uModel * vec4(aPos, 1.0);
    FragPos = worldPosition.xyz;
    TexCoord = aTexCoord;

    mat3 normalMat = mat3(uNormalCol0.xyz, uNormalCol1.xyz, uNormalCol2.xyz);
    Normal = normalize(normalMat * aNormal);

    gl_Position = uProjViewMat * worldPosition;
}
