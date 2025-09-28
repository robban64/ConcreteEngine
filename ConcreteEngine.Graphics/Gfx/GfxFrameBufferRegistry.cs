using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Gfx.Internal;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Resources;




public sealed class GfxFrameBufferRegistry
{
    public FrameBufferLayout RegisterFrameBufferScreen(GfxFrameBufferDescriptor desc)
    {
        var record = RegisterInternal(desc);
        record.CalculateSize =  null;
        record.FixedSize = null;

        _frameBuffers.Add(record.FboId, record);
        return record.ToLayout();
    }
    
    public FrameBufferLayout RegisterFrameBufferFixed(GfxFrameBufferDescriptor desc, Size2D fixedSize)
    {
        var record = RegisterInternal(desc);
        record.CalculateSize =  null;
        record.FixedSize = fixedSize;
        
        _frameBuffers.Add(record.FboId, record);
        return record.ToLayout();
    }

    public FrameBufferLayout RegisterFrameBufferCalc(GfxFrameBufferDescriptor desc, Vector2 ratio, CalcFboSizeDel calcSizeCallback)
    {
        var record = RegisterInternal(desc);
        record.CalculateSize =  calcSizeCallback;
        record.CalculateRatio = ratio;
        record.FixedSize = null;
        
        _frameBuffers.Add(record.FboId, record);
        return record.ToLayout();
    }

    private FrameBufferRecord RegisterInternal(GfxFrameBufferDescriptor desc)
    {
        var fboId = _gfx.CreateFrameBuffer(in desc);
        var meta = _resources.FboStore.GetMeta(fboId);

        return new FrameBufferRecord(fboId, in meta);
    }

    internal void RecreateFrameBuffers(Size2D outputSize)
    {
        var fboStore = _resources.FboStore;
        foreach (var fboId in fboStore.IdEnumerator)
        {
            var record = _frameBuffers[fboId];
            
            if (record.FixedSize is { } fixedSize)
                Update(record, fixedSize);
            else if (record.CalculateSize is { } calculate)
                Update(record, calculate(outputSize, record.CalculateRatio));
            else 
                Update(record, outputSize);
        }
    }

    private void Update(FrameBufferRecord record, Size2D newSize)
    {
        if(record.OutputSize == newSize) return;
        record.OutputSize = newSize;
        _gfx.RecreateFrameBuffer(record.FboId, newSize);
    }

    private sealed class FrameBufferRecord(FrameBufferId fboId, in FrameBufferMeta meta)
    {
        public FrameBufferId FboId { get; } = fboId;
        public FboAttachmentIds Attachments { get; } = meta.Attachments;
        public RenderBufferMsaa Msaa { get; } = meta.MultiSample;
        public Size2D? FixedSize { get; set; }
        public CalcFboSizeDel? CalculateSize { get; set; }
        public Vector2 CalculateRatio { get; set; } = Vector2.One;
        public Size2D OutputSize { get; set; }

        public FrameBufferLayout ToLayout() => new (FboId, Attachments,Msaa);
    }
}
public readonly record struct FboAttachmentIds(
    TextureId ColorTextureId,
    TextureId DepthTextureId,
    RenderBufferId ColorRenderBufferId,
    RenderBufferId DepthRenderBufferId
);
public readonly record struct FrameBufferLayout(
    FrameBufferId FboId,
    FboAttachmentIds Attachments,
    RenderBufferMsaa Msaa);
