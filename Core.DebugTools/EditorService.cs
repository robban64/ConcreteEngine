using System.Numerics;
using ConcreteEngine.Common.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Gui;
using Core.DebugTools.Utils;
using ImGuiNET;

namespace Core.DebugTools;

public sealed class EditorService
{
    private readonly MetricService _metricService;
    private readonly DevConsoleService _devConsole;

    private readonly Topbar _topbar;
    private readonly LeftSidebar _leftSidebar;
    private readonly RightSidebar _rightSidebar;

    private readonly EditorStateContext _stateContext;
    
    public DevConsoleService DevConsole => _devConsole;

    public EditorService(MetricService metricService)
    {
        _metricService = metricService;
        _stateContext = new EditorStateContext(_metricService);
        _devConsole = new DevConsoleService();
        _topbar = new Topbar(_stateContext);
        _leftSidebar = new LeftSidebar(_metricService, _stateContext);
        _rightSidebar = new RightSidebar(_metricService, _stateContext);
    }


    public void Render()
    {
        _topbar.Draw();
        if (_stateContext.ViewMode != EditorViewMode.None)
        {
            _leftSidebar.Draw(240, offset: GuiTheme.TopbarHeight);
            _rightSidebar.Draw(160, offset: GuiTheme.TopbarHeight);
        }
        _devConsole.Draw(240, 160);
    }
}