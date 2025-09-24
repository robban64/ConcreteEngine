#region

using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Manifest;

public static class UniformsStd140Layouts
{
    public static readonly IReadOnlyDictionary<UniformGpuSlot, string> Map =
        new Dictionary<UniformGpuSlot, string>
        {
            { UniformGpuSlot.Frame, FrameGlobalUniform },
            { UniformGpuSlot.Camera, CameraUniform },
            { UniformGpuSlot.DirLight, DirLightUniform },
            { UniformGpuSlot.Material, MaterialUniform },
            { UniformGpuSlot.DrawObject, DrawUniform }
        };

    private const string FrameGlobalUniform =
        """
        layout(std140, binding = 0) uniform FrameGlobalUniform {
            vec4 uAmbient;   // xyz=color, w=intensity
            vec4 uFogColor;  // xyz=color, w=density
            vec4 uFogDetail; // x=near, y=far, z=type, w=0
        };
        """;

    private const string CameraUniform =
        """
        layout(std140, binding = 1) uniform CameraUniform {
            mat4 uViewMat;
            mat4 uProjMat;
            mat4 uProjViewMat;
            vec4 uCameraPos; // C# has vec3 + float pad; use .xyz
        };
        """;

    private const string DirLightUniform =
        """
        layout(std140, binding = 2) uniform DirLightUniform {
            vec4 uLightDirection;            // xyz, w unused
            vec4 uLightDiffuse;              // rgb, w unused
            vec4 uLightSpecularIntensity;    // xyz=specular, w=intensity
        };
        """;

    private const string MaterialUniform =
        """
        layout(std140, binding = 3) uniform MaterialUniform {
            vec3 MaterialColor;
            float Shininess;
            float SpecularStrength;
            float uvRepeat;                
            vec2 _materialPad0;
        };
        """;

    private const string DrawUniform =
        """
        layout(std140, binding = 4) uniform DrawUniform {
            mat4 uModel;
            // normal matrix as vec4 (xyz used)
            vec4 uNormalCol0;
            vec4 uNormalCol1;
            vec4 uNormalCol2;
        };
        """;
}