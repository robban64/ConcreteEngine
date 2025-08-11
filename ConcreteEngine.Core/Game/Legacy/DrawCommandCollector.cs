using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Game.Legacy;

internal sealed class DrawCommandCollector
{
    private readonly SortedList<int, IDrawCommandEmitter> _emitters = new(16);

    public void RegisterEmitter<T>(T emitter) where T : class, IDrawCommandEmitter
    {
        if (_emitters.ContainsValue(emitter)) throw new InvalidOperationException("Duplicated emitter");
        if (_emitters.ContainsKey(emitter.Order)) throw new InvalidOperationException($"Order {emitter.Order} is already registered");

        _emitters.Add(emitter.Order, emitter);
    }

    public void Collect(IGraphicsContext ctx, DrawCommandSubmitter submitter)
    {
        foreach (var emitter in _emitters.Values)
        {
            emitter.Emit(ctx, submitter);
        }
    }
    
}

