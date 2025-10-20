#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Renderer.Data;

public readonly record struct MaterialPassState(
    DepthMode DepthTest,
    CullMode CullMode,
    BlendMode BlendMode,
    bool DepthWrite
);