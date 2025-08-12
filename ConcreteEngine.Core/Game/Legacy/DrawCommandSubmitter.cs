using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Rendering;

namespace ConcreteEngine.Core.Game.Legacy;

public sealed class DrawCommandSubmitter
{
    private readonly TypeRegistryCollection<object> _queueRegistry = new();

    public void RegisterCommand<T>() where T : IDrawCommand, new()
    {
        //TODO use different capacity for different buckets
        _queueRegistry.Register<T>(new List<T>(32));
    }

    public void SubmitDraw<T>(in T cmd, in DrawCommandMeta meta) where T : unmanaged, IDrawCommandMessage
    {
        var queue = _queueRegistry.Get<T>();
        // Direct cast; no boxing. JIT will devirtualize Enqueue.
        //TODO
        ((List<(T cmd, DrawCommandMeta meta)>)queue).Add((cmd, meta));

    }
}