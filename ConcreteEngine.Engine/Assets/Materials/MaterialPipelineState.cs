#region

using ConcreteEngine.Graphics.Gfx.Contracts;

#endregion

namespace ConcreteEngine.Engine.Assets.Materials;

public readonly record struct MaterialPipelineState(GfxPassState PassState, GfxPassStateFunc PassFunctions);