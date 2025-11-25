using ConcreteEngine.Graphics.Gfx.Contracts;

namespace ConcreteEngine.Engine.Assets.Materials;

public readonly record struct MaterialPipelineState(GfxPassState PassState, GfxPassStateFunc PassFunctions);
