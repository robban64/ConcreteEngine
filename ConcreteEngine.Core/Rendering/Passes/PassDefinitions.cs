using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public enum RenderTargetId : byte
{
    Scene,
    Shadow,
    Light,
    PostEffect,
    Screen
}

public enum PassOpKind : byte
{
    Normal = 0,
    Resolve = 1,
    Fsq = 2,
    Screen = 3
}

public readonly record struct ApplyPassReturn(PassOpKind OpKind)
{
    public static ApplyPassReturn BeginNormalPass() => new(PassOpKind.Normal);
    public static ApplyPassReturn ResolveTarget() => new(PassOpKind.Resolve);
    public static ApplyPassReturn BeginFsqPass() => new(PassOpKind.Fsq);
    public static ApplyPassReturn BeginScreenPass() => new(PassOpKind.Screen);

}

