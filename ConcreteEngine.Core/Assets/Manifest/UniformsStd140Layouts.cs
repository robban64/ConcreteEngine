#region

#endregion

namespace ConcreteEngine.Core.Assets.Manifest;

internal sealed class UniformsStd140Layouts
{
    public IReadOnlyDictionary<string, string> Map => _map;

    private readonly Dictionary<string, string> _map;

    public UniformsStd140Layouts()
    {
        _map = new Dictionary<string, string>
        {
            { "Frame", _frameGlobalUniform },
            { "Camera", _cameraUniform },
            { "DirLight", _dirLightUniform },
            { "Material", _materialUniform },
            { "DrawObject", _drawUniform }
        };
    }

    public void Cleanup()
    {
        _map.Clear();
    }

    private string _frameGlobalUniform =
        """
        layout(std140, binding = 0) uniform FrameGlobalUniform {
            vec4 uAmbient;   // xyz=color, w=intensity
            vec4 uFogColor;  // xyz=color, w=density
            vec4 uFogDetail; // x=near, y=far, z=type, w=0
        };
        """;

    private string _cameraUniform =
        """
        layout(std140, binding = 1) uniform CameraUniform {
            mat4 uViewMat;
            mat4 uProjMat;
            mat4 uProjViewMat;
            vec4 uCameraPos; // C# has vec3 + float pad; use .xyz
        };
        """;

    private string _dirLightUniform =
        """
        layout(std140, binding = 2) uniform DirLightUniform {
            vec4 uLightDirection;            // xyz, w unused
            vec4 uLightDiffuse;              // rgb, w unused
            vec4 uLightSpecularIntensity;    // xyz=specular, w=intensity
        };
        """;

    private string _materialUniform =
        """
        layout(std140, binding = 3) uniform MaterialUniform {
            vec3 MaterialColor;
            float Shininess;
            float SpecularStrength;
            float uvRepeat;                
            vec2 _materialPad0;
        };
        """;

    private string _drawUniform =
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