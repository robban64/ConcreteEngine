namespace ConcreteEngine.Core.Game.Legacy;

public interface IDrawCommandMessage;

public readonly struct DrawCommandMeta(int layer, int target)
{
    public readonly int Layer = layer;
    public readonly int Target = target;
}

public readonly record struct EmitterMessage<T>(in T Cmd, in DrawCommandMeta Info)
    where T : unmanaged, IDrawCommandMessage;