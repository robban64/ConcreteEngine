using ConcreteEngine.Graphics.Gfx.Contracts;

namespace ConcreteEngine.Core.Renderer.Material;

public readonly record struct MaterialPipelineState(GfxPassState PassState, GfxPassFunctions PassFunctions);