using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public sealed class FrameEntityStore : IDisposable
{ 
    public int VisibleCount { get; private set; }

    private NativeArray<RenderEntityId> _visibleEntities;
    
    internal FrameEntityStore(int capacity)
    {
        _visibleEntities = NativeArray.Allocate<RenderEntityId>(capacity);
    }
    
    public NativeView<RenderEntityId> VisibleEntities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _visibleEntities.Slice(0, VisibleCount);
    }
    public ReadOnlySpan<RenderEntityId> VisibleSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _visibleEntities.AsReadOnlySpan(0, VisibleCount);
    }

    internal NativeView<RenderEntityId> WriteVisibleEntities()
    {
        if ((uint)RenderEcs.Core.Count > (uint)_visibleEntities.Length)
            Throwers.BufferOverflow(nameof(_visibleEntities));

        VisibleCount = 0;
        return _visibleEntities.Slice(0, RenderEcs.Core.Count);
    }
    
    internal void CommitFrame(int visibleCount)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)visibleCount,  (uint)_visibleEntities.Length);
        if(!_visibleEntities[visibleCount - 1].IsValid()) 
            Throwers.InvalidArgument(nameof(visibleCount));
        
        VisibleCount = visibleCount;
    }

    internal void Resize(int newSize)
    {
        if(newSize <= _visibleEntities.Length) return;
        _visibleEntities.Resize(newSize, true);
    }
    

    public void Dispose() => _visibleEntities.Dispose();
}