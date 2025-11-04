using Core.DebugTools.Data;
using Core.DebugTools.Gui;
using ImGuiNET;

namespace Core.DebugTools;

public sealed class EditorService
{
    private readonly MetricService _metricService;

    private readonly EditorLeftPanel _leftPanel;
    private readonly DebugRightPanelGui _rightPanel;
    
    private readonly EditorViewState  _viewState;

    public EditorService(MetricService metricService)
    {
        _metricService = metricService;
        _viewState = new EditorViewState();
        _leftPanel = new EditorLeftPanel(_metricService, _viewState);
        _rightPanel = new DebugRightPanelGui(_metricService.TextData);
    }
    
    public void Render()
    {
        _leftPanel.Draw(240);
        _rightPanel.DrawRight(160);
    }
}