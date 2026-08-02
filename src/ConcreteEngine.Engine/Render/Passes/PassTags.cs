namespace ConcreteEngine.Engine.Render.Passes;

public interface IRenderTarget
{
   static abstract RenderTargetKind TargetKind { get; }
}

public struct SceneTarget : IRenderTarget
{
    public static RenderTargetKind TargetKind => RenderTargetKind.Scene;
}

public struct ShadowTarget : IRenderTarget
{
    public static RenderTargetKind TargetKind => RenderTargetKind.Shadow;
}

public struct LightTarget : IRenderTarget
{
    public static RenderTargetKind TargetKind => RenderTargetKind.Light;
}

public struct PostFxTarget : IRenderTarget
{
    public static RenderTargetKind TargetKind => RenderTargetKind.Screen;
}

public struct OutputTarget : IRenderTarget
{
    public static RenderTargetKind TargetKind => RenderTargetKind.Screen;
}

public struct ScreenPassTag : IRenderTarget
{
    public static RenderTargetKind TargetKind => RenderTargetKind.Screen;
}