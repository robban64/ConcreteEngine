namespace ConcreteEngine.Renderer.Passes;

public readonly record struct PassAction(PassOp Op)
{
    public static PassAction DrawPassResult() => new(PassOp.Draw);
    public static PassAction DrawEffectPassResult() => new(PassOp.DrawEffect);
    public static PassAction ResolveTargetResult() => new(PassOp.Resolve);
    public static PassAction FsqPassResult() => new(PassOp.Fsq);
    public static PassAction ScreenPassResult() => new(PassOp.Screen);
}