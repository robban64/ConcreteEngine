using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector.Impl;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.UI;

internal sealed class VisualPanel(StateManager state) : EditorPanel(PanelId.Visual, state)
{
    private readonly InspectPostFxFields _inspectFields = InspectorFieldProvider.Instance.PostFxFields;

    public override void OnEnter(ref MemoryBlockPtr memory) => _inspectFields.Refresh();

    public override void OnDraw()
    {
        _inspectFields.Draw();
    }
}