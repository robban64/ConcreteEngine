using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Inspector;

internal abstract class AssetInspectorUi
{
    public void DrawSpecification(EditorAsset editAsset, in FrameContext ctx)
    {
        ImGui.SeparatorText(ref ctx.Sw.Write(editAsset.Kind.ToText()));
    }

    public void Draw()
    {
        
    }
}