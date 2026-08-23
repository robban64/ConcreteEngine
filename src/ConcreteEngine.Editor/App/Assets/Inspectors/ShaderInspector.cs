using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets;

internal sealed class ShaderInspector : Inspector<Shader>
{
    public required StateManager State { get; init; }
    public override InspectorId Id => InspectorId.Asset;
    public override uint Icon { get; }

    public ShaderInspector()
    {
    }

    public override void Draw()
    {
        ImGui.Spacing();
        var width = AppLayout.GetRowWidthForItems(2);
        if (ImGui.Button("Open"u8, new Vector2(width, 0))) ;

        ImGui.SameLine();

        if (ImGui.Button("Reload"u8, new Vector2(width, 0)))
            State.EnqueueEvent(new AssetEvent(Target!.Id, Target!.Kind, Reload: true));

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Recompiles source files."u8);
    }
}