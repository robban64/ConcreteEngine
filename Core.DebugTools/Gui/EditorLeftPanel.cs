using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Gui.Metrics;
using ImGuiNET;

namespace Core.DebugTools.Gui;

internal sealed class EditorLeftPanel
{
    private readonly MetricReport _metricReport;
    private readonly AssetStoreGui _assetStoreGui;

    public LeftPanelMode Mode { get; set; }

    public EditorLeftPanel(MetricReport metricReport, AssetStoreViewModel viewModel)
    {
        _metricReport = metricReport;
        _assetStoreGui = new AssetStoreGui(viewModel);
    }

    public void Draw(int width)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        var vp = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(width, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        ImGui.SetNextWindowBgAlpha(0.95f);

        if (ImGui.Begin("##LeftSidebar", flags))
        {
            DrawModeSelector();

            ImGui.Separator();

            switch (Mode)
            {
                case LeftPanelMode.Metrics: DrawMetrics(); break;
                case LeftPanelMode.Editor: DrawEditor(); break;
            }
        }

        ImGui.End();
        ImGui.PopStyleVar(2);
    }

    private void DrawModeSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));
        if (ImGui.BeginTabBar("left_panel_tabs", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("Main"))
            {
                Mode = LeftPanelMode.Editor;
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("Metrics"))
            {
                Mode = LeftPanelMode.Metrics;
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
        ImGui.PopStyleVar();
    }

    public void DrawEditor()
    {
        _assetStoreGui.DrawLeft();
    }


    private void DrawMetrics()
    {
        SceneMetricsGui.DrawSceneMetrics(_metricReport.SceneMetrics);
        ImGui.Dummy(new Vector2(0, 6));
        AssetStoreMetricsGui.DrawAssetStoreMetrics(_metricReport);
        ImGui.Dummy(new Vector2(0, 6));
        GfxStoreMetricsGui.DrawGfxStoreMetrics(_metricReport);
    }
}