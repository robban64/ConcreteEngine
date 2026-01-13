using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;
using ZaString.Core;

namespace ConcreteEngine.Editor.Components.Draw;

internal static class DrawAssets
{
    public static void CategoryChanged(AssetState state, AssetKind kind)
    {
        if (kind == state.SelectedKind) return;
        state.SelectedKind = kind;

        if (kind == AssetKind.Unknown) state.ResetState();
    }

    public static void DrawAssetTypeSelector(AssetState state, int length, ref ZaUtf8SpanWriter za)
    {
        var category = state.SelectedKind;

        var currentLabel = category.ToTextUtf8();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);

        za.Clear();
        if (ImGui.BeginCombo("##assetTypeSelector"u8, currentLabel, ImGuiComboFlags.HeightLargest))
        {
            for (var i = 0; i < length; i++)
            {
                var isSelected = i == (int)category;
                var kind = (AssetKind)i;

                if (ImGui.Selectable(kind.ToTextUtf8(), isSelected, ImGuiSelectableFlags.None, Vector2.Zero))
                    CategoryChanged(state, kind);

                if (isSelected)
                    ImGui.SetItemDefaultFocus();

                za.Clear();
            }
            ImGui.EndCombo();
        }
    }
    public static void DrawAssetKindTag(AssetKind kind)
    {
        var color = kind switch {
            AssetKind.Shader => new Vector4(0.392f, 0.584f, 0.929f, 1.0f),
            AssetKind.Model  => new Vector4(1f, 0.647f, 0f, 1.0f),
            AssetKind.Texture  => new Vector4(0.4f, 0.4f, 0.8f, 1.0f),
            AssetKind.Material  => new Vector4(0.4f, 0.8f, 0.4f, 1.0f),
            _ => Vector4.One
        };
        ImGui.TextColored(color, kind.ToShortTextUtf8()); 
    }

}