namespace ConcreteEngine.Engine.Render.Passes;

public interface IRenderTarget
{
    
}
public struct ScenePassTag : IRenderTarget;

public struct ShadowPassTag : IRenderTarget;

public struct LightPassTag : IRenderTarget;

public struct PostPassTag : IRenderTarget;

public struct OutputPassTag : IRenderTarget;

public struct ScreenPassTag : IRenderTarget;