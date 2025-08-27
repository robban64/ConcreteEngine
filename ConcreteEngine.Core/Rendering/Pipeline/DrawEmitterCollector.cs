#region

using ConcreteEngine.Core.Rendering.Emitters;

#endregion

namespace ConcreteEngine.Core.Rendering.Pipeline;

internal sealed class DrawEmitterCollector
{
    private readonly SortedList<int, IDrawCommandEmitter> _emitters = new(8);

    public int Count => _emitters.Count;

    public DrawCommandEmitter<TDrawData> GetEmitter<TEmitter, TDrawData>()
        where TEmitter : DrawCommandEmitter<TDrawData>
        where TDrawData : class
    {
        foreach (var (_, emitter) in _emitters)
        {
            if (emitter is TEmitter tEmitter) return tEmitter;
        }

        throw new InvalidOperationException($"Emitter {typeof(TEmitter).Name} not registered");
    }

    public IDrawCommandEmitter GetEmitter(Type emitterType)
    {
        foreach (var (_, emitter) in _emitters)
        {
            if (emitter.GetType() == emitterType) return emitter;
        }

        throw new InvalidOperationException($"Emitter {emitterType.Name} not registered");
    }

    public void AddEmitter(int order, IDrawCommandEmitter emitter)
    {
        ArgumentNullException.ThrowIfNull(emitter, nameof(emitter));
        if (_emitters.ContainsValue(emitter))
            throw new InvalidOperationException($"Emitter {emitter.GetType().Name} is already registered");

        _emitters.Add(order, emitter);
    }

    public void Initialize()
    {
        foreach (var (order, emitter) in _emitters)
        {
            emitter.Initialize(order);
        }
    }

    public void Collect(DrawEmitterContext context, DrawCommandSubmitter submitter)
    {
        var emitters = _emitters.Values;
        foreach (var emitter in emitters)
        {
            emitter.Emit(context, submitter);
        }
    }
}