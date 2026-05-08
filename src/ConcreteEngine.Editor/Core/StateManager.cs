using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Resources;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Core;

internal sealed class StateManager(EventDispatcher eventDispatcher, GfxResourceApi gfxApi)
{
    public event Action<EditorContext, EditorContext>? ContextChanged;

    public EditorContext Context = new() { Mode = new ModeContext { Id = ModeId.Asset } };
    public int ActiveDebugWindow { get; private set; } = -1;

    public void ToggleDebugWindow(int id)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(id, WindowManager.DebugWindowCount);

        if (ActiveDebugWindow >= 0 && ActiveDebugWindow == id) id = -1;

        MetricSystem.Instance.FastMode = id >= 0;
        if (id >= 0) MetricSystem.Instance.Stores?.Refresh();

        ActiveDebugWindow = id;
    }

    public void EmitChange(EditorContext context)
    {
        if (Context == context)
        {
            ConsoleGateway.LogPlain("Identical context emitted");
            return;
        }

        var prev = Context;
        Context = context;
        ContextChanged?.Invoke(prev, context);
    }

    public void EnqueueEvent<TEvent>(TEvent evt) where TEvent : EditorEvent => eventDispatcher.Enqueue(evt);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetOrSetTextureHandle(TextureId id, scoped ref TexturePtrHandle texHandle)
    {
        var handle = gfxApi.GetNativeHandle<TextureId, TextureMeta>(id);
        if (texHandle.Handle == handle) return;

        if (!texHandle.TexturePtr.IsNull) texHandle.TexturePtr.Destroy();
        texHandle.TexturePtr = ImGui.ImTextureRef(new ImTextureID(handle.Value));
        texHandle.Handle = handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTextureRefPtr(TextureId id, out ImTextureRefPtr refPtr)
    {
        if (!id.IsValid())
        {
            refPtr = default;
            return false;
        }

        var handle = gfxApi.GetNativeHandle<TextureId, TextureMeta>(id);
        refPtr = ImGui.ImTextureRef(new ImTextureID(handle.Value));
        return true;
    }
}