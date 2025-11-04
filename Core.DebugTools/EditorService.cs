using System.Numerics;
using ConcreteEngine.Common.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Gui;
using Core.DebugTools.Utils;
using ImGuiNET;

namespace Core.DebugTools;

public sealed class EditorService
{
    private readonly MetricService _metricService;

    private readonly EditorLeftPanel _leftPanel;
    private readonly DebugRightPanelGui _rightPanel;
    private readonly DevConsoleService _devConsole;

    private readonly EditorStateContext _stateContext;

    public EditorService(MetricService metricService)
    {
        _metricService = metricService;
        _stateContext = new EditorStateContext();
        _devConsole = new DevConsoleService();
        _leftPanel = new EditorLeftPanel(_metricService, _stateContext);
        _rightPanel = new DebugRightPanelGui(_metricService.TextData);
    }


    public void Render()
    {
        
        TopbarGui.Draw();
        _leftPanel.Draw(240, offset: GuiTheme.TopbarHeight);
        _rightPanel.DrawRight(160, offset: GuiTheme.TopbarHeight);
        _devConsole.Draw(240, 160);
    }
}