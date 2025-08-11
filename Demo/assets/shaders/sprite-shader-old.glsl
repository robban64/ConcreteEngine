@vertex
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoord;

uniform mat4 uModel;
uniform mat4 uViewProj;

// For texture atlas
uniform vec2 uTexOffset; // UV offset in atlas
uniform vec2 uTexScale;  // UV scale in atlas

out vec2 TexCoord;

void main()
{
  gl_Position = uViewProj * uModel * vec4(aPos, 0.0, 1.0);
  TexCoord = aTexCoord * uTexScale + uTexOffset;
}

@fragment
#version 330 core
in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D uTexture;

void main()
{
    FragColor = texture(uTexture, TexCoord);
}