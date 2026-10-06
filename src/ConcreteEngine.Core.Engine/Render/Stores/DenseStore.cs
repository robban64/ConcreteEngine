using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;

public sealed unsafe class DenseStore<T> : RenderStore where T : unmanaged, IRenderComponent<T>
{ 
    public int Capacity { get; private set; }
    
    private T* _ptr;
    private NativeArray<byte> _data;
    
    private readonly bool _zeroed;

    public DenseStore(int capacity, bool zeroed)
    {
        Capacity = capacity;
        _zeroed = zeroed;
        _data = NativeArray.Allocate<byte>(capacity * Unsafe.SizeOf<T>(), zeroed);
        _ptr = (T*)_data.Ptr;
    }
    
    public ref T this[int entity]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)entity >= (uint)Capacity) Throwers.IndexOutOfRange(entity, Capacity, nameof(entity));
            return ref _ptr[entity];
        }
    }
    
    public ref T this[RenderEntity entity]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)entity.Id >= (uint)Capacity) Throwers.IndexOutOfRange(entity.Id, Capacity, nameof(entity));
            return ref _ptr[entity.Id];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => new(_ptr, RenderWorld.EntityCount);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan() => new(_ptr, RenderWorld.EntityCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> AsView() => new(_ptr, RenderWorld.EntityCount);

    internal override void OnDenseResized(int newSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(newSize, Capacity);
        Capacity = newSize;
        _data.ReAlloc(newSize * Unsafe.SizeOf<T>(), _zeroed);
        _ptr = (T*)_data.Ptr;
    }

    public override void Dispose() => _data.Dispose();
}