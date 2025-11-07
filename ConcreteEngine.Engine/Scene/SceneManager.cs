#region

using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Descriptors;

#endregion

namespace ConcreteEngine.Engine.Scene;

internal sealed class SceneManager
{
    private readonly List<Func<GameScene>> _sceneFactories;
    private int _pendingIndex = -1;

    public GameScene? Current { get; private set; }

    public bool HasPendingSwitch => _pendingIndex >= 0;

    public SceneManager(List<Func<GameScene>> sceneFactories)
    {
        _sceneFactories = sceneFactories ?? throw new ArgumentNullException(nameof(sceneFactories));
    }

    public void QueueSwitch(int sceneIndex) => _pendingIndex = sceneIndex;

    public void ApplyPendingScene(
        GameSceneContext context,
        GameSceneConfigBuilder builder,
        Action<SceneBuildResult>? afterBuild)
    {
        if (_pendingIndex < 0) return;

        var index = _pendingIndex;
        if (index >= _sceneFactories.Count)
            throw new IndexOutOfRangeException($"Switch scene, index {index} is out of range.");

        Current?.Unload();

        var newScene = _sceneFactories[index]();
        newScene.AttachContext(context);

        builder.Clear();
        newScene.Build(builder);

        afterBuild?.Invoke(new SceneBuildResult(
            builder.RenderType,
            builder.RenderTargetsDesc,
            builder.Modules,
            context));

        newScene.InitializeInternal();

        Current = newScene;
        _pendingIndex = -1;
        builder.Clear();
    }

    internal sealed class SceneBuildResult(
        RenderType renderType,
        RenderTargetDescriptor renderTargetsDesc,
        IReadOnlyList<Func<GameModule>> modules,
        GameSceneContext context)
    {
        public RenderType RenderType { get; } = renderType;
        public RenderTargetDescriptor RenderTargetsDesc { get; } = renderTargetsDesc;
        public IReadOnlyList<Func<GameModule>> Modules { get; } = modules;
        public GameSceneContext Context { get; } = context;
    }
}