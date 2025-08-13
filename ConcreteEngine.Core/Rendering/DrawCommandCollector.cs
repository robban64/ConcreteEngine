using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawCommandCollector()
{
    private readonly SortedList<int, IDrawCommandEmitter> _emitters = new(8);

    public int Count => _emitters.Count;
    
    public void RegisterEmitter<T>(int order, T emitter) where T : class, IDrawCommandEmitter
    {
        ArgumentNullException.ThrowIfNull(emitter, nameof(emitter));
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        if (_emitters.ContainsValue(emitter)) throw new InvalidOperationException("Duplicated emitter");
        if (_emitters.ContainsKey(order)) throw new InvalidOperationException($"Order {emitter.Order} is already registered");
        
        emitter.Order = order;
        _emitters.Add(order, emitter);
    }

    public void Collect(DrawEmitterContext context, DrawCommandSubmitter submitter)
    {
        foreach (var emitter in _emitters.Values)
        {
            emitter.Emit(context, submitter);
        }
    }
    
}

