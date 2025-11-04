using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Gui.Metrics;
using Core.DebugTools.Utils;
using ImGuiNET;

namespace Core.DebugTools.Gui;

internal sealed class EditorLeftPanel
{
    private readonly MetricService _metricService;
    private readonly AssetStoreGui _assetStoreGui;
    private readonly EditorStateContext _stateContext;

    public EditorLeftPanel(MetricService metricService, EditorStateContext stateContext)
    {
        _metricService = metricService;
        _stateContext = stateContext;
        _assetStoreGui = new AssetStoreGui(stateContext, OnSelectionChanged, OnAssetSelectedChanged);
    }

    private void OnAssetSelectedChanged(AssetObjectViewModel? asset)
    {
        var model = _stateContext.AssetViewModel;
        model.AssetFileObjects.Clear();
        if (asset is null) return;
        EditorTable.FetchAssetObjectFiles?.Invoke(asset, model.AssetFileObjects);
    }

    private void OnSelectionChanged(EditorAssetSelection selection)
    {
        var model = _stateContext.AssetViewModel;

        if (selection == model.TypeSelection) return;
        model.TypeSelection = selection;
        model.AssetObjects.Clear();
        model.AssetFileObjects.Clear();

        if (selection == EditorAssetSelection.None) return;

        EditorTable.FillAssetStoreView?.Invoke(selection, model.AssetObjects);
    }

    private void OnModeChanged(LeftPanelMode mode)
    {
        if (mode == _stateContext.LeftMode) return;
        _stateContext.LeftMode = mode;

        switch (_stateContext.LeftMode)
        {
            case LeftPanelMode.Editor:
                _metricService.ActiveStoreMetrics = false;
                _metricService.ActiveSceneMetrics = false;
                OnSelectionChanged(_stateContext.AssetViewModel.TypeSelection);
                return;
            case LeftPanelMode.Metrics:
                _metricService.ActiveStoreMetrics = true;
                _metricService.ActiveSceneMetrics = true;
                break;
        }
    }

    public void Draw(int width, int offset)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                       ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
        var vp = ImGui.GetMainViewport();

        var height = _stateContext.LeftMode == LeftPanelMode.None ? 0 : vp.WorkSize.Y - offset;

        ImGui.SetNextWindowPos(new Vector2(0, offset));
        ImGui.SetNextWindowSize(new Vector2(width, height));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##LeftSidebar", flags))
        {
            DrawModeSelector();

            ImGui.Separator();

            switch (_stateContext.LeftMode)
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
            if (ImGui.BeginTabItem("None"))
            {
                if (_stateContext.LeftMode != LeftPanelMode.None) OnModeChanged(LeftPanelMode.None);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Assets"))
            {
                if (_stateContext.LeftMode != LeftPanelMode.Editor) OnModeChanged(LeftPanelMode.Editor);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Metrics"))
            {
                if (_stateContext.LeftMode != LeftPanelMode.Metrics) OnModeChanged(LeftPanelMode.Metrics);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar();
    }

    private void DrawEditor() => _assetStoreGui.Draw();

    private void DrawMetrics()
    {
        var metrics = _metricService.TextData;
        SceneMetricsGui.DrawSceneMetrics(metrics.SceneMetrics);
        ImGui.Dummy(new Vector2(0, 6));
        AssetStoreMetricsGui.DrawAssetStoreMetrics(metrics);
        ImGui.Dummy(new Vector2(0, 6));
        GfxStoreMetricsGui.DrawGfxStoreMetrics(metrics);
    }
}