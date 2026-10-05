using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;

namespace ConcreteEngine.Core.Engine.Render;


public abstract class RenderCoreArray : IDisposable
{
    public abstract void SetDefaultValueFor(int entity);
    internal abstract void Resize(int newLength);

    public abstract void Dispose();
}

public sealed class RenderCoreArray<T> : RenderCoreArray where T : unmanaged
{
    public static T DefaultValue = default;
    
    private readonly bool _zeroed;
    
    private NativeArray<T> _data;

    public RenderCoreArray(int capacity, bool zeroed)
    {
        _zeroed = zeroed;
        _data = NativeArray.Allocate<T>(capacity);
    }
    
    public ref T this[int entity]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)entity >= (uint)_data.Length) Throwers.IndexOutOfRange(entity, _data.Length, nameof(entity));
            return ref _data[entity];
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => _data.AsSpan(0, RenderWorld.EntityCount);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AsReadOnlySpan() => _data.AsReadOnlySpan(0, RenderWorld.EntityCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<T> AsView() => _data.Slice(0, RenderWorld.EntityCount);

    public override void SetDefaultValueFor(int entity) => this[entity] = DefaultValue;

    internal override void Resize(int newLength)
    {
        _data.ReAlloc(newLength, _zeroed);
    }

    public override void Dispose() => _data.Dispose();
}