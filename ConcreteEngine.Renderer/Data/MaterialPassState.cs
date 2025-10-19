#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct MaterialPassState(
    DepthMode DepthTest,
    CullMode CullMode,
    BlendMode BlendMode,
    bool DepthWrite
);