namespace ConcreteEngine.Core.Rendering.Passes;

public interface IRenderPassBaseTag;

public interface IRenderPassTag : IRenderPassBaseTag;

public readonly struct ScenePassTag : IRenderPassTag;

public readonly struct ShadowPassTag : IRenderPassTag;

public readonly struct LightPassTag : IRenderPassTag;

public readonly struct PostPassTag : IRenderPassTag;

public readonly struct ScreenPassTag : IRenderPassTag;

public interface IRenderPassTagSlot : IRenderPassBaseTag;

public interface IRenderPassSceneSlot : IRenderPassTagSlot;

public readonly struct ScenePassDrawSlot : IRenderPassSceneSlot;

public readonly struct ScenePassResolveSlot : IRenderPassSceneSlot;

public interface IPostPassSlot : IRenderPassTagSlot;

public readonly struct PostPassASlot : IPostPassSlot;

public readonly struct PostPassBSlot : IPostPassSlot;

public interface IScreenPassSlot : IRenderPassTagSlot;

public readonly struct ScreenPassPresentSlot : IScreenPassSlot;