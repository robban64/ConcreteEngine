using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public enum PassTargetKind : byte
{
    None,
    Draw,
    ResolveTo,
    SampleInPass
}

public enum AttachmentKind : byte
{
    Color0,
    Color1,
    Depth
}

public readonly record struct ApplyPassReturn(PassTargetKind Target)
{
    public static ApplyPassReturn BeginDrawPass() => new(PassTargetKind.Draw);
    public static ApplyPassReturn BeginSamplePass() => new(PassTargetKind.SampleInPass);
    public static ApplyPassReturn ResolveTarget() => new(PassTargetKind.ResolveTo);
}

public readonly struct PassReturn
{
    public static readonly PassReturn None = default;

    public readonly FrameBufferId ResolveFboId;
    public readonly TextureId SourceTexture;
    public readonly RenderBufferId SourceBuffer;
    public readonly int PassIndex;
    public readonly byte Slot;
    public readonly PassTargetKind Kind;
    public readonly RenderTargetId TargetId;
    public readonly AttachmentKind Attachment;
    public bool IsNone => Kind == PassTargetKind.None;

    private PassReturn(PassTargetKind kind, RenderTargetId targetId, int passIndex, byte slot,
        FrameBufferId resolveFboId,
        TextureId sourceTexture,
        RenderBufferId sourceBuffer, AttachmentKind attachment = AttachmentKind.Color0)
    {
        Kind = kind;
        TargetId = targetId;
        PassIndex = passIndex;
        Attachment = attachment;
        SourceTexture = sourceTexture;
        SourceBuffer = sourceBuffer;
        ResolveFboId = resolveFboId;
        Slot = slot;
    }

    public static PassReturn ResolveTo(RenderTargetId id, int passIndex, byte slot, FrameBufferId resolveFboId) =>
        new(PassTargetKind.ResolveTo, id, passIndex, slot, resolveFboId, default, default);

    public static PassReturn SampleIn(RenderTargetId id, int passIndex, byte slot,
        TextureId colTex, RenderBufferId depthBuff = default) =>
        new(PassTargetKind.SampleInPass, id, passIndex, slot, default, colTex, depthBuff);
}
/*
public readonly struct PassReturn
{
    public static readonly PassReturn None = default;

    public readonly bool HasValue;
    public readonly NextAction Action;

    private PassReturn(in NextAction action)
    {
        HasValue = !action.IsNone;
        Action = action;
    }


    public static implicit operator PassReturn(NextAction action) => new(in action);

    public static PassReturn ResolveTo(RenderTargetId id, int passIndex, AttachmentKind att) =>
        NextAction.ResolveTo(id, passIndex, att);

    public static PassReturn SampleIn(RenderTargetId id, int passIndex, AttachmentKind att) =>
        NextAction.SampleIn(id, passIndex, att);
}*/