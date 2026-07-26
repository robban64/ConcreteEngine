using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxDraw 
{
    private MeshId _boundMeshId;
    private RenderFrameMeta _frameMeta;


    internal GfxDraw()
    {
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
        GlStates.BindMesh(default);
        result = _frameMeta;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindDraw(MeshId id, uint instanceCount = 0)
    {
        if (_boundMeshId != id)
        {
            _boundMeshId = id;
            GlStates.BindMesh(GfxRegistry.GetStore<MeshMeta>().GetHandle(id));
        }

        ref readonly var meta = ref GfxRegistry.GetStore<MeshMeta>().GetMeta(id);
        if (meta.Kind < DrawMeshKind.ArraysInstanced)
        {
            GlStates.Draw(in meta);
        }
        else
        {
            instanceCount = uint.Max(meta.InstanceCount, instanceCount);
            GlStates.DrawInstance(in meta, instanceCount);
        }
        _frameMeta.AddDrawCall(meta.DrawCount, instanceCount);
    }

}