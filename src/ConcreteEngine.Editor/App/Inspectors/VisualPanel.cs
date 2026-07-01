using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Inspector;
using ConcreteEngine.Editor.Core.Inspector.Impl;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed class VisualPanel(StateManager state) : EditorPanel(InspectorId.Visual, state)
{
    private readonly InspectPostFxFields _inspectFields = InspectorFieldProvider.Instance.PostFxFields;

    public override void OnEnter() => _inspectFields.Refresh();

    public override void OnDraw()
    {
        _inspectFields.Draw();
    }
}