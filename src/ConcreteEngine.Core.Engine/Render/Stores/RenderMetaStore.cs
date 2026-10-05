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

    private BitSet _entitySet;
    private BitSet _visibleSet;
    
    private NativeArray<ulong> _sortKeys;

    internal RenderMetaStore(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, MinCapacity);

        Capacity = capacity;

        _entitySet = new BitSet(capacity);
        _visibleSet = new BitSet(capacity);

        _generations = new ushort[capacity];
        _sortKeys = NativeArray.Allocate<ulong>(capacity);
    }

    //
    public BitSet EntitySet => _entitySet;
    public BitSet VisibleSet => _visibleSet;

    public ReadOnlySpan<ushort> GenerationSpan() => new(_generations, 0, RenderWorld.EntityCount);

    public NativeView<ulong> RawSortKeys => _sortKeys;
    public NativeView<DrawEntityKey> SortKeys => RawSortKeys.Reinterpret<DrawEntityKey>();

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(int entity) => _entitySet[entity];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(int entity) => _visibleSet[entity];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetGeneration(int entity) => _generations[entity];
    //

    //
    internal RenderEntity AddEntity(int entity, DrawPolicy policy, DrawSource source)
    {
        if (_entitySet[entity]) Throwers.InvalidArgument("Entity already exists");
        _entitySet[entity] = true;

        Policies[entity] = policy;
        Sources[entity] = source;
        WorldBounds[entity] = default;
        Transforms[entity].Transform = Matrix4x4.Identity;
        Normals[entity].Normal = Matrix3X4.Identity;

        var gen = ++_generations[entity];
        return new RenderEntity(entity, gen);
    }

    internal void RemoveEntity(RenderEntity entity)
    {
        if (!IsAlive(entity.Id)) Throwers.InvalidArgument("Bug: Entity is already dead", nameof(entity));

        var generation = _generations[entity.Id];
        if (entity.Gen != generation) Throwers.InvalidArgument(nameof(entity), "Bug: Entity generation mismatch");
        _entitySet[entity.Id] = false;
        Policies[entity.Id] = default;
        Sources[entity.Id] = default;
    }


    //
    internal void Resize(int newSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(newSize, Capacity);

        Array.Resize(ref _generations, newSize);
        _sortKeys.ReAlloc(newSize, true);

        if (newSize > _entitySet.BitCount)
        {
            _entitySet = _entitySet.Resized(newSize);
            _visibleSet = _visibleSet.Resized(newSize);
        }

        Capacity = newSize;
    }


    public void Dispose()
    {
        _sortKeys.Dispose();
        Capacity = 0;
    }
    
    public  DenseStore<DrawPolicy> Policies => RenderWorld.Dense<DrawPolicy>();
    public  DenseStore<DrawSource> Sources => RenderWorld.Dense<DrawSource>();
    public  DenseStore<WorldBox> WorldBounds => RenderWorld.Dense<WorldBox>();
    public  DenseStore<WorldTransform> Transforms => RenderWorld.Dense<WorldTransform>();
    public  DenseStore<NormalMatrix> Normals => RenderWorld.Dense<NormalMatrix>();

}