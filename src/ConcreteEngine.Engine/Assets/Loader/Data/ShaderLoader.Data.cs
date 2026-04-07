using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader.Data;

internal readonly struct ShaderCreationInfo(ShaderId shaderId, int samplers)
{
    public readonly ShaderId ShaderId = shaderId;
    public readonly int Samplers = samplers;
}