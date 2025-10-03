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
            { "Light", _lightUniform },
            { "Shadow", _shadowUniform },
            { "Material", _materialUniform },
            { "DrawObject", _drawUniform },
            { "PostProcess", _postProcessUniform },
        };
    }

    public void Cleanup()
    {
        _map.Clear();
    }

    private string _frameGlobalUniform =
        """
        layout(std140, binding = 0) uniform FrameGlobalUniform {
            vec4 uAmbient;  
            vec4 uAmbientGround;
            vec4 uFogColor; 
            vec4 uFogParams0;
            vec4 uFogParams1;
        };
        """;

    private string _cameraUniform =
        """
        layout(std140, binding = 1) uniform CameraUniform {
            mat4 uViewMat;
            mat4 uProjMat;
            mat4 uProjViewMat;
            vec4 uCameraPos;
        };
        """;

    private string _dirLightUniform =
        """
        layout(std140, binding = 2) uniform DirLightUniform {
            vec4 uLightDirection;           
            vec4 uLightDiffuse;             
            vec4 uLightSpecularIntensity;   
        };
        """;

    private string _lightUniform =
        """
        layout(std140, binding = 3) uniform LightUniform {
            ivec4 uLightCounts;         // x = lightCount (0..MAX_LIGHTS), yzw reserved
            LightData uLights[MAX_LIGHTS];
        };
        """;

    private string _shadowUniform =
        """
        layout(std140, binding = 4) uniform ShadowUniform {
            mat4 uLightViewProj;
            vec4 uShadowParams0;   // x=1/texW, y=1/texH, z=constBias, w=slopeBias
            vec4 uShadowParams1;   // x=strength, y=pcfRadius, z,w reserved
        };
        """;

    private string _materialUniform =
        """
        layout(std140, binding = 5) uniform MaterialUniform {
            vec4 uMatColor;
            vec4 uMatParams0;
            vec4 uMatParams1;
        };
        """;

    private string _drawUniform =
        """
        layout(std140, binding = 6) uniform DrawUniform {
            mat4 uModel;
            // normal matrix as vec4 (xyz used)
            vec4 uNormalCol0;
            vec4 uNormalCol1;
            vec4 uNormalCol2;
        };
        """;

    private string _postProcessUniform =
        """
        layout(std140, binding = 7) uniform PostProcessUniform {
            vec4 ColorAdjust;
            vec4 WhiteBalance;
            vec4 Flags;
            vec4 BloomParams;
            vec4 BloomLods;
            vec4 LutParams;
            //
            vec4 VignetteParams;
            vec4 GrainParams;
            vec4 ChromAbParams;
            //
            vec4 ToneShadows;
            vec4 ToneHighlights;
            vec4 SharpenParams;
        };
        """;
}