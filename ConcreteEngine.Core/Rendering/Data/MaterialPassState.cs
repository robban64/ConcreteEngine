using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Core.Rendering.Data;

public readonly record struct MaterialPassState(
    DepthMode DepthTest,
    CullMode CullMode,
    BlendMode BlendMode,
    bool DepthWrite
);