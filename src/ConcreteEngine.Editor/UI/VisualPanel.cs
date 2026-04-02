using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Impl;

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