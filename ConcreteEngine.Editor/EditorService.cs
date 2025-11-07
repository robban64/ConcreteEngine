using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

public static class EditorService
{
    static EditorService()
    {
        StateCtx.Init();
        CameraPropertyComponent.Init();
    }


    public static void Render()
    {
        var viewState = StateCtx.PreRender();
        Topbar.Draw();

        if (!viewState.IsEmptyViewMode)
        {
            LeftSidebar.Draw(240, offset: GuiTheme.TopbarHeight);
            RightSidebar.Draw(GuiTheme.RightSidebarWidth, offset: GuiTheme.TopbarHeight);

        }

        DevConsoleService.Draw(240, GuiTheme.RightSidebarWidth);
    }
}