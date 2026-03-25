using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Definition;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Bridge.EngineObjectStore;

namespace ConcreteEngine.Editor.UI;

internal sealed class VisualPanel(StateContext context) : EditorPanel(PanelId.Visual, context)
{
    private readonly InspectPostFxFields _inspectFields = InspectorFieldProvider.Instance.PostFxFields;

    public override void OnEnter() => _inspectFields.Refresh();

    public override void OnDraw(FrameContext ctx)
    {
       _inspectFields.Draw();
    }


}