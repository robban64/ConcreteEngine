using ConcreteEngine.Common.Collections;

namespace ConcreteEngine.Core.Rendering;

internal sealed class DrawEmitterCollector
{
    private readonly SortedList<int, IDrawCommandEmitter> _emitters = new(8);

    public int Count => _emitters.Count;

    public DrawCommandEmitter<TEntity> GetEmitter<TEmitter, TEntity>()
        where TEmitter : DrawCommandEmitter<TEntity>
        where TEntity : struct
    {
        foreach (var emitter in _emitters.Values)
        {
            if (emitter is TEmitter tEmitter) return tEmitter;
        }

        throw new InvalidOperationException($"Emitter {typeof(TEmitter).Name} not registered");
    }

    public void RegisterEmitter<TEmitter, TEntity>(int order, TEmitter emitter)
        where TEmitter : DrawCommandEmitter<TEntity>
        where TEntity : struct
    {
        ArgumentNullException.ThrowIfNull(emitter, nameof(emitter));
        ArgumentOutOfRangeException.ThrowIfNegative(order, nameof(order));
        if (_emitters.ContainsValue(emitter)) throw new InvalidOperationException("Duplicated emitter");
        if (_emitters.ContainsKey(order))
            throw new InvalidOperationException($"Order {emitter.Order} is already registered");

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