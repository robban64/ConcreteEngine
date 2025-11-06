using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

public sealed class EditorService
{
    private readonly DevConsoleService _devConsole;

    private readonly Topbar _topbar;
    private readonly LeftSidebar _leftSidebar;
    private readonly RightSidebar _rightSidebar;

    private readonly EditorStateContext _stateContext;
    
    public DevConsoleService DevConsole => _devConsole;

    public EditorService()
    {
        _devConsole = new DevConsoleService();
        _stateContext = new EditorStateContext(DevConsole);
        _topbar = new Topbar(_stateContext);
        _leftSidebar = new LeftSidebar(_stateContext);
        _rightSidebar = new RightSidebar(_stateContext);
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