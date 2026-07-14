using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed class VisualPanel(StateManager state) : EditorPanel(InspectorId.Visual, state)
{
    private readonly PostEffectSettingsInspector _postEffectSettingsInspector = new();

    public override void OnDraw()
    {
        _postEffectSettingsInspector.Draw();
    }
}