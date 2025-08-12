using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Rendering;

public interface IDrawCommandMessage;

public readonly struct DrawCommandMeta(RenderTargetId pass, short layer)
{
    public readonly RenderTargetId Pass = pass;
    public readonly short Layer = layer;
}

public readonly record struct EmitterMessage<T>(in T Cmd, in DrawCommandMeta Info)
    where T : unmanaged, IDrawCommandMessage;