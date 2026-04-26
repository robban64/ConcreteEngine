using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Resources;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Core;

internal sealed class StateManager(
    EventManager eventManager,
    SelectionManager selection,
    GfxResourceApi gfxApi)
{
    public Action<EditorContext, EditorContext>? ContextChanged;

    public readonly SelectionManager Selection = selection;
    public EditorContext Context = new() { Mode = new ModeContext { IsMetricMode = false } };


    public void EmitChange(EditorContext context)
    {
        if(Context == context)
        {
            ConsoleGateway.LogPlain("Identical context emitted");
            return;
        }

        var prev = Context;
        Context = context;
        ContextChanged?.Invoke(prev, context);
        
        if (prev.Selection != context.Selection)
            Selection.SelectionContextChange(context.Selection);

        if (prev.Tool.ShowDebugBounds != context.Tool.ShowDebugBounds)
            Selection.ToggleDrawBounds(context.Tool.ShowDebugBounds);
    }

    public void EnqueueEvent<TEvent>(TEvent evt) where TEvent : EditorEvent => eventManager.Enqueue(evt);

    public bool TryGetTextureRefPtr(TextureId id, out ImTextureRefPtr refPtr)
    {
        if (!id.IsValid())
        {
            refPtr = default;
            return false;
        }

        refPtr = ImGui.ImTextureRef(new ImTextureID(gfxApi.GetNativeHandle(id)));
        return true;
    }
}