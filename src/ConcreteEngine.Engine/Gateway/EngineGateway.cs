using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Renderer;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class EngineGateway : IDisposable
{
    public bool Enabled { get; private set; }

    public readonly EngineMetricHub Metrics;

    private EditorPortal _editor = null!;

    internal EngineGateway()
    {
        Metrics = new EngineMetricHub();
    }

    public void SetupEditor(CommandBus commandBus, GfxContext gfxContext)
    {
        ArgumentNullException.ThrowIfNull(commandBus);
        ArgumentNullException.ThrowIfNull(gfxContext);

        if (Enabled) throw new InvalidOperationException(nameof(Enabled));
        if (_editor != null) throw new InvalidOperationException("Editor is already setup.");

        Enabled = true;

        _editor = new EditorPortal();
        Metrics.ConnectEditor(_editor.GetMetricSystem());

        _editor.Start();
        Logger.BindLogger(_editor.GetLogBindings());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginFrame()
    {
        if (!Enabled) return;
        _editor.UpdateInput();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderEditor(float deltaTime)
    {
        if (!Enabled) return;
        _editor.Render(deltaTime, RenderContext.Instance.OutputTexture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateGameTick(float deltaTime)
    {
        if (!Enabled) return;
        _editor.UpdateGameTick(deltaTime);
    }

    public void UpdateDiagnostics(float delta)
    {
        if (!Enabled) return;
        Metrics.OnDiagnosticTick();
        _editor.OnDiagnosticTick();
    }

    public void Dispose()
    {
        Enabled = false;
        _editor.Dispose();
    }
}