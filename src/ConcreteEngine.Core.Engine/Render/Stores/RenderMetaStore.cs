using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed class RenderMetaStore : IDisposable
{
    public const int MinCapacity = 128;

    public int Capacity { get; private set; }

    private ushort[] _generations;

    private BitSet64 _entitySet;
    private BitSet64 _visibleSet;
    
    private BitSet64 _trueSet;

    internal RenderMetaStore(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, MinCapacity);

        Capacity = capacity;

        _generations = new ushort[capacity];

        _entitySet = new BitSet64(capacity);
        _visibleSet = new BitSet64(capacity);
        _trueSet = new BitSet64(capacity);
        _trueSet.SetAll(true);
    }

    //
    public BitSet64 EntitySet => _entitySet;
    public BitSet64 VisibleSet => _visibleSet;
    public BitSet64 TrueSet => _trueSet;

    public ReadOnlySpan<ushort> GenerationSpan() => new(_generations, 0, RenderWorld.EntityCount);

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetGeneration(int entity) => _generations[entity];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(int entity) => _entitySet[entity];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(int entity) => _visibleSet[entity];


    //
    internal RenderEntity AddEntity(int entity, DrawPolicy policy, DrawSource source)
    {
        if (_entitySet[entity]) Throwers.InvalidArgument("Entity already exists");
        _entitySet[entity] = true;

        RenderWorld.Dense<DrawPolicy>()[entity] = policy;
        RenderWorld.Dense<DrawSource>()[entity] = source;
        RenderWorld.Dense<WorldBox>()[entity] = default;
        RenderWorld.Dense<WorldTransform>()[entity].Transform = Matrix4x4.Identity;
        RenderWorld.Dense<NormalMatrix>()[entity].Normal = Matrix3X4.Identity;

        var gen = ++_generations[entity];
        return new RenderEntity(entity, gen);
    }

    internal void RemoveEntity(RenderEntity entity)
    {
        if (!IsAlive(entity.Id)) Throwers.InvalidArgument("Bug: Entity is already dead", nameof(entity));

        var generation = _generations[entity.Id];
        if (entity.Gen != generation) Throwers.InvalidArgument(nameof(entity), "Bug: Entity generation mismatch");
        _entitySet[entity.Id] = false;
        RenderWorld.Dense<DrawPolicy>()[entity.Id] = default;
        RenderWorld.Dense<DrawSource>()[entity.Id] = default;
    }


    //
    internal void Resize(int newSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(newSize, Capacity);

        Array.Resize(ref _generations, newSize);

        if (newSize > _entitySet.BitCount)
        {
            _entitySet = _entitySet.Resized(newSize);
            _visibleSet = _visibleSet.Resized(newSize);
            _trueSet = _trueSet.Resized(newSize);
            _trueSet.SetAll(true);
        }

        Capacity = newSize;
    }


    public void Dispose()
    {
        Capacity = 0;
    }

}