namespace ConcreteEngine.Engine.Render.Passes;

public interface IRenderTarget;

public struct SceneTarget : IRenderTarget;

public struct ShadowTarget : IRenderTarget;

public struct LightTarget : IRenderTarget;

public struct PostFxTarget : IRenderTarget;

public struct OutputTarget : IRenderTarget;

public struct ScreenPassTag : IRenderTarget;