using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

public sealed unsafe class GfxDraw : IDisposable
{
    private static NativeArray<byte> _tableMemory;

    private MeshId _boundMeshId;
    private MeshMeta _boundMeshMeta;
    private RenderFrameMeta _frameMeta;

    private readonly GlStates _states;
    private readonly GfxResourceStore<MeshId, MeshMeta> _meshStore;

    private readonly delegate*<DrawPrimitive, DrawElementSize, uint, uint, void>* _drawTable;

    internal GfxDraw(GfxContextInternal ctx)
    {
        if(!_tableMemory.IsNull) throw new InvalidOperationException("DrawTable is already initialized.");
        
        _states = ctx.Driver.States;
        _meshStore = ctx.Resources.GfxStoreHub.MeshStore;
        
        _tableMemory = NativeArray.Allocate<byte>(sizeof(nint) * 4);

        _drawTable = (delegate*<DrawPrimitive, DrawElementSize, uint, uint, void>*)_tableMemory.Ptr;
        _drawTable[0] = &GlDraw.DrawInvalid;
        _drawTable[1] = &GlDraw.DrawArrays;
        _drawTable[2] = &GlDraw.DrawElements;
        _drawTable[3] = &GlDraw.DrawInstanced;


    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BeginFrame()
    {
        _frameMeta = default;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EndFrame(out RenderFrameMeta result)
    {
        _boundMeshId = default;
        _boundMeshMeta = default;
        BindMesh(default);
        result = _frameMeta;
    }
    
    public void BindMesh(MeshId id)
    {
        if (_boundMeshId == id) return;

        if (id == default)
        {
            _states.UnbindMesh();
            _boundMeshId = default;
            _boundMeshMeta = default;
            return;
        }

        var handle = _meshStore.GetHandleAndMeta(id, out _boundMeshMeta);
        _states.BindMesh(handle);
        _boundMeshId = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindDraw(MeshId id, uint instanceCount = 0)
    {
        ref var meta = ref _boundMeshMeta;
        if (_boundMeshId != id)
        {
            _boundMeshId = id;
            var handle = _meshStore.GetHandleAndMeta(id, out meta);
            _states.BindMesh(handle);
        }
        
        var instances = uint.Max(meta.InstanceCount, instanceCount);
        _drawTable[(int)meta.Kind](meta.Primitive, meta.ElementSize, meta.DrawCount, meta.InstanceCount);
        _frameMeta.AddDrawCall(meta.DrawCount, instances);
    }

    public void Dispose() => _tableMemory.Dispose();
}