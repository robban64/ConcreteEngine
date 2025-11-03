using System.Numerics;
using Core.DebugTools.Data;
using ImGuiNET;

namespace Core.DebugTools.Components;

internal sealed class AssetStoreGui
{
    public AssetStoreViewModel ViewModel { get; } = new();
    
    private bool _isDirty = true;

    public void Refresh()
    {
        _isDirty = true;
    }

    public void Draw()
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);

        ImGui.SetNextWindowSize(new Vector2(300f, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        ImGui.SetNextWindowBgAlpha(0.95f);

        var flags =
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        if (ImGui.Begin("##LeftSidebar", flags))
        {
            CommonComponents.DrawSectionHeader("Asset Store");
        }

        foreach (var asset in ViewModel.AssetObjects)
        {
            ImGui.TextUnformatted(asset.Name);
            ImGui.TextUnformatted(asset.AssetId.ToString());
            ImGui.TextUnformatted(asset.IsCoreAsset.ToString());
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

}