using Core.DebugTools.Data;
using Core.DebugTools.Gui;
using ImGuiNET;

namespace Core.DebugTools;

public sealed class EditorService
{
    private readonly MetricService _metricService;

    private readonly EditorLeftPanel _leftPanel;
    private readonly DebugRightPanelGui _rightPanel;
    
    private readonly EditorStateContext  _stateContext;

    public EditorService(MetricService metricService)
    {
        _metricService = metricService;
        _stateContext = new EditorStateContext();
        _leftPanel = new EditorLeftPanel(_metricService, _stateContext);
        _rightPanel = new DebugRightPanelGui(_metricService.TextData);
    }
    
    public void Render()
    {
        _leftPanel.Draw(240);
        _rightPanel.DrawRight(160);
    }
}