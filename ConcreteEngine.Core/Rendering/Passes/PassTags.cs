namespace ConcreteEngine.Core.Rendering.Passes;


public interface IRenderPassTag;

public readonly struct ScenePassTag : IRenderPassTag;

public readonly struct ShadowPassTag : IRenderPassTag;

public readonly struct LightPassTag : IRenderPassTag;

public readonly struct PostPassTag : IRenderPassTag;

public readonly struct ScreenPassTag : IRenderPassTag;


