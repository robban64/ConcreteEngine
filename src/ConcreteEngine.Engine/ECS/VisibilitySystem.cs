using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.ECS;

namespace ConcreteEngine.Engine.ECS;

[Flags]
public enum VisibilityFlags : byte
{
    None          = 0,
    CullFrustum   = 1 << 0,
    CullOcclusion = 1 << 1,
    ToggledOff    = 1 << 2,
}

internal unsafe struct VisibilityCallback
{
    public delegate*<RenderEntityId, bool, void> OnVisibilityChanged;
}

internal sealed class VisibilitySystem
{
    public const int MaxCapacity = 1024 * 50;

    private bool[] _visibleIndices = [];
    private VisibilityCallback[] _callbacks = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void PushVisible(RenderEntityId entity, bool visible)
    {
        var index = entity.Index();
        var lastVisible = _visibleIndices[index];
        if(lastVisible == visible) return;
        _visibleIndices[index] = visible;
        
        var callback = _callbacks[index].OnVisibilityChanged;
        if(callback != null) callback(entity, visible);
    }
/*
    private RenderEntityId[] _visibleEntities = [];
    private int[] _entityToVisibleIndex = [];
    private int _index;
    
    public void Prepare()
    {
        if (Ecs.Render.Core.Capacity > _visibleEntities.Length)
            EnsureCapacity();

        if (_visibleEntities.Length != _entityToVisibleIndex.Length)
            throw new InvalidOperationException($"{nameof(VisibilitySystem)} array length mismatch");

        _entityToVisibleIndex.AsSpan(0, Ecs.Render.Core.Count).Fill(-1);

        _index = 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushVisible(RenderEntityId entity)
    {
        _visibleEntities[_index] = entity;
        _entityToVisibleIndex[entity.Index()] = _index;
        _index++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(RenderEntityId entity)
    {
        var index = entity.Index();
        if((uint)index >= (uint)_entityToVisibleIndex.Length) throw new ArgumentOutOfRangeException(nameof(entity));
        return _visibleEntities[_entityToVisibleIndex[index]].IsValid();
    }
    
    public void EnsureCapacity()
    {
        var len = _visibleEntities.Length;
        var newCap = Arrays.CapacityGrowthSafe(len, Ecs.Render.Core.Capacity);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException($"{nameof(VisibilitySystem)} Buffer exceeded max limit");

        _visibleEntities = new RenderEntityId[newCap];
        _entityToVisibleIndex = new int[newCap];

        if (len > 0)
            Logger.LogString(LogScope.Engine, $"{nameof(VisibilitySystem)} buffer resize {newCap}", LogLevel.Warn);
    }


    public static readonly VisibilitySystem Instance = new();

    public static void Dispatch(RenderEntityId entity, bool visible)
    {
        var idx = Instance._entityToVisibleIndex[entity];
        
    }
    */
}