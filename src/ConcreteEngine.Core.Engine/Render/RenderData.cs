using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Render;


public sealed class RenderData : IDisposable
{
    public const int MinCapacity = 128;

    private static class CoreArrays<T> where T : unmanaged
    {
        public static RenderCoreArray<T> Array { get; private set; } = null!;

        public static RenderCoreArray<T> Create(int capacity, bool zeroed)
        {
            if (Array != null!) Throwers.InvalidOperation("CoreArray already created");
            Array = new RenderCoreArray<T>(capacity, zeroed);
            _coreArrays.Add(Array);
            return Array;
        }
    }

    private static readonly List<RenderCoreArray> _coreArrays = new(8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RenderCoreArray<T> GetArray<T>() where T : unmanaged => CoreArrays<T>.Array;

    public int Capacity { get; private set; }

    private BitSet _entitySet;
    private BitSet _visibleSet;

    private ushort[] _generations;
    private NativeArray<ulong> _sortKeys;

    public readonly RenderCoreArray<DrawPolicy> Policies;
    public readonly RenderCoreArray<DrawSource> Sources;

    public readonly RenderCoreArray<BoundingAxisBox> WorldBounds;
    public readonly RenderCoreArray<Matrix4x4> Transforms;
    public readonly RenderCoreArray<Matrix3X4> Normals;


    internal RenderData(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, MinCapacity);

        Capacity = capacity;

        _ = _coreArrays;
        
        _entitySet = new BitSet(capacity);
        _visibleSet = new BitSet(capacity);

        _generations = new ushort[capacity];
        _sortKeys = NativeArray.Allocate<ulong>(capacity);

        Policies = CoreArrays<DrawPolicy>.Create(capacity, true);
        Sources = CoreArrays<DrawSource>.Create(capacity, true);
        WorldBounds = CoreArrays<BoundingAxisBox>.Create(capacity, false);
        Transforms = CoreArrays<Matrix4x4>.Create(capacity, false);
        Normals = CoreArrays<Matrix3X4>.Create(capacity, false);
        
        RenderCoreArray<Matrix4x4>.DefaultValue = Matrix4x4.Identity;
        RenderCoreArray<Matrix3X4>.DefaultValue = Matrix3X4.Identity;

    }

    //
    public BitSet EntitySet => _entitySet;
    public BitSet VisibleSet => _visibleSet;

    public ReadOnlySpan<ushort> GenerationSpan => new(_generations, 0, RenderWorld.EntityCount);

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
        Transforms[entity] = Matrix4x4.Identity;
        Normals[entity] = Matrix3X4.Identity;

        var gen = ++_generations[entity];
        return new RenderEntity(entity, gen);
    }

    internal void RemoveEntity(RenderEntity entity)
    {
        if (!IsAlive(entity.Id)) Throwers.InvalidArgument("Entity is already dead", nameof(entity));

        var generation = _generations[entity.Id];
        if (entity.Gen != generation) Throwers.InvalidArgument(nameof(entity), "Entity generation mismatch");
        _entitySet[entity.Id] = false;
        Policies[entity.Id] = default;
        Sources[entity.Id] = default;
    }


    //

    internal void ReAlloc(int newSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(newSize, Capacity);

        Array.Resize(ref _generations, newSize);
        _sortKeys.ReAlloc(newSize, true);

        foreach (var array in _coreArrays)
            array.Resize(newSize);

        if (newSize > _entitySet.BitCount)
        {
            _entitySet = _entitySet.Resized(newSize);
            _visibleSet = _visibleSet.Resized(newSize);
        }

        Capacity = newSize;
    }


    public void Dispose()
    {
        foreach (var array in _coreArrays)
        {
            array.Dispose();
        }

        _sortKeys.Dispose();

        Capacity = 0;
    }
}