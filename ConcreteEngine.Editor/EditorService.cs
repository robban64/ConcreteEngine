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

    private readonly EditorStateContext _ctx;
    
    public DevConsoleService DevConsole => _devConsole;

    public EditorService()
    {
        _devConsole = new DevConsoleService();
        _ctx = new EditorStateContext(DevConsole);
        _topbar = new Topbar(_ctx);
        _leftSidebar = new LeftSidebar(_ctx);
        _rightSidebar = new RightSidebar(_ctx);
        
        CameraPropertyGui.Init(_ctx);
    }


    public void Render()
    {
        _ctx.PreRender();
        
        _topbar.Draw();
        if (_ctx.ViewMode != EditorViewMode.None)
        {
            _leftSidebar.Draw(240, offset: GuiTheme.TopbarHeight);
            
            if(_ctx.PropertyMode != RightSidebarMode.None)
                _rightSidebar.Draw(GuiTheme.RightSidebarWidth, offset: GuiTheme.TopbarHeight);
        }
        _devConsole.Draw(240, GuiTheme.RightSidebarWidth);
    }
}