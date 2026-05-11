using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector.Impl;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.UI;

internal sealed class VisualPanel(StateManager state) : EditorPanel(StateEnums.Visual, state)
{
    private readonly InspectPostFxFields _inspectFields = InspectorFieldProvider.Instance.PostFxFields;

    public override void OnEnter(NativeAllocator allocator) => _inspectFields.Refresh();

    public override void OnDraw()
    {
        _inspectFields.Draw();
    }
}