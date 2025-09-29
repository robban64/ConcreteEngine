namespace ConcreteEngine.Core.Rendering;

public enum NextActionKind : byte
{
    None,
    ResolveTo,
    SampleInPass
}

public enum AttachmentKind : byte
{
    Color0,
    Color1,
    Depth
}

public readonly struct NextAction
{
    public static readonly NextAction None = default;

    public readonly NextActionKind Kind;
    public readonly RenderTargetId TargetId;
    public readonly int PassIndex;
    public readonly AttachmentKind Attachment;

    public bool IsNone => Kind == NextActionKind.None;

    private NextAction(NextActionKind kind, RenderTargetId targetId, int passIndex, AttachmentKind attachment)
    {
        Kind = kind;
        TargetId = targetId;
        PassIndex = passIndex;
        Attachment = attachment;
    }

    public static NextAction ResolveTo(RenderTargetId id, int passIndex, AttachmentKind att) =>
        new(NextActionKind.ResolveTo, id, passIndex, att);

    public static NextAction SampleIn(RenderTargetId id, int passIndex, AttachmentKind att) =>
        new(NextActionKind.SampleInPass, id, passIndex, att);
}

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
}