using System.Drawing;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Rendering;

public readonly record struct RegisterRenderTargetDesc(
    RenderTargetId Target,
    short Order,
    Vector2D<float> SizeRatio,
    bool DoClear,
    Color ClearColor,
    ClearBufferFlag ClearMask,
    RenderPassResolveTarget ResolveTo,
    RenderTargetKey ResolveToTarget
);

public sealed class RenderPass
{
    public required RenderTargetKey GfxKey { get; init; }
    public required Vector2D<float> SizeRatio { get; set; }
    public required RenderTargetId Target { get; init; }
    public required int Order { get; init; }
    public required bool DoClear { get; init; }
    public required Color ClearColor { get; init; }
    public required ClearBufferFlag ClearMask { get; init; }
    public required RenderPassResolveTarget ResolveTo { get; init; }
    public required RenderTargetKey ResolveToTarget { get; init; }
    public required ushort ShaderId { get; init; }


    internal static RenderPass From(RenderTargetKey key, Shader? shader, in RegisterRenderTargetDesc desc)
    {
        return new RenderPass
        {
            GfxKey = key,
            SizeRatio = desc.SizeRatio,
            Order = desc.Order,
            Target = desc.Target,
            DoClear = desc.DoClear,
            ClearColor = desc.ClearColor,
            ClearMask = desc.ClearMask,
            ResolveTo = desc.ResolveTo,
            ResolveToTarget = desc.ResolveToTarget,
            ShaderId = shader?.ResourceId ?? 0
        };
    }
}