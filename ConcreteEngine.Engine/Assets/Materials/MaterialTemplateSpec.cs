#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Engine.Assets.Materials;

public sealed class MaterialTemplateSpec
{
    public ShadingModelMode ShadingModel { get; }
    public AlphaMode AlphaMode { get; }

    public bool TwoSided { get; }
    public SurfaceNormalMode NormalUsage { get; }

    public bool CastShadows { get; }
    public bool ReceiveShadows { get; }

    public DepthMode DepthTest { get; }
    public CullMode CullMode { get; }
    public BlendMode BlendMode { get; }
    public bool DepthWrite { get; }

    public PassMask PassMask { get; }
}