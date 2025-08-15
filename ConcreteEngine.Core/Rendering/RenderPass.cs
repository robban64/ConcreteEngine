using System.Drawing;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

public sealed class RenderPass
{
    public ushort FboId { get; set; }
    public required Vector2D<int> Size { get; set; }
    public required RenderTargetId Target { get; init; }
    public required int Order { get; init; }
    public required bool Clear { get; init; }
    public required Color ClearColor { get; init; }
    public required ClearBufferFlag ClearMask { get; init; }
    public required RenderPassResolveTarget ResolveTo { get; init; }
    public required RenderPass? ResolveToFbo { get; init; }
    //public ushort ResolveToFboId { get; init; }
    
    internal static RenderPass From(in RenderPassDesc desc, RenderPass? resolveToFbo = null)
    {
        return new RenderPass
        {
            FboId = desc.FboId,
            Size = desc.Size,
            Order = desc.Order,
            Target = desc.Target,
            Clear = desc.Clear,
            ClearColor = desc.ClearColor,
            ClearMask = desc.ClearMask,
            ResolveTo = desc.ResolveTo,
            ResolveToFbo = resolveToFbo
        };
    }
}