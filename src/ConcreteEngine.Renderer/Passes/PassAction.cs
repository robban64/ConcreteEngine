using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Renderer.Passes;

public readonly record struct PassAction(PassOpKind OpKind)
{
    public static PassAction DrawPassResult() => new(PassOpKind.Draw);
    public static PassAction DrawEffectPassResult() => new(PassOpKind.DrawEffect);
    public static PassAction ResolveTargetResult() => new(PassOpKind.Resolve);
    public static PassAction FsqPassResult() => new(PassOpKind.Fsq);
    public static PassAction ScreenPassResult() => new(PassOpKind.Screen);
}