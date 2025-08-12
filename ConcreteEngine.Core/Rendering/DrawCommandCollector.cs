using ConcreteEngine.Core.Rendering.Sprite;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawCommandCollector(int initialCapacity = 16)
{
    private readonly SortedList<int, IDrawCommandEmitter> _emitters = new(initialCapacity);

    public int Count => _emitters.Count;
    
    public void RegisterEmitter<T>(T emitter) where T : class, IDrawCommandEmitter
    {
        if (_emitters.ContainsValue(emitter)) throw new InvalidOperationException("Duplicated emitter");
        if (_emitters.ContainsKey(emitter.Order)) throw new InvalidOperationException($"Order {emitter.Order} is already registered");

        _emitters.Add(emitter.Order, emitter);
    }

    public void Collect(DrawEmitterContext context, DrawCommandSubmitter submitter)
    {
        foreach (var emitter in _emitters.Values)
        {
            emitter.Emit(context, submitter);
        }
    }
    
}

