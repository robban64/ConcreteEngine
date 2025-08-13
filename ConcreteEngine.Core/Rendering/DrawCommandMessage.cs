using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Rendering;

public interface IDrawCommandMessage;

public readonly struct DrawCommandMeta(RenderTargetId pass, short layer)
{
    public readonly RenderTargetId Pass = pass;
    public readonly short Layer = layer;
}

public readonly struct EmitterMessage<T>(in T cmd, in DrawCommandMeta info)
    where T : unmanaged, IDrawCommandMessage
{
    public readonly T Cmd = cmd;
    public readonly DrawCommandMeta Info;
}