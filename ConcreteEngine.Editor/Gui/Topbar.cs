using System.Numerics;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.Gui;

internal sealed class Topbar
{
    private const int SelectorWidth = 74;
    private static readonly string[] ViewModes = Enum.GetNames<EditorViewMode>();
    private static readonly string[] PropertyModes = Enum.GetNames<RightSidebarMode>();

    private readonly EditorStateContext _ctx;

    public Topbar(EditorStateContext ctx)
    {
        _ctx = ctx;
    }

    private void OnViewModeChanged(EditorViewMode mode) => _ctx.SetViewMode(mode);
    private void OnPropertyModeChange(RightSidebarMode mode) => _ctx.SetPropertyMode(mode);

    public void Draw()
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                                       ImGuiWindowFlags.NoResize;

        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(vp.Size.X, GuiTheme.TopbarHeight));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.SetNextWindowBgAlpha(GuiTheme.PanelOpacity);

        if (ImGui.Begin("##TopBar", flags))
        {
            ImGui.SetWindowFontScale(1.06f);
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GuiTheme.SelectedColor);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, GuiTheme.SelectedColor);
            ImGui.PushStyleColor(ImGuiCol.Header, GuiTheme.PrimaryColor);

            // left
           DrawModeSelector();
            
            // right
            DrawPropertySelector();
            
            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar(1);
        }

        ImGui.End();
        ImGui.PopStyleVar(1);
    }

    private void DrawModeSelector()
    {
        if (!ImGui.BeginChild("##editor-view-mode-selector")) return;
        for (var i = 0; i < ViewModes.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            var selected = i == (int)_ctx.ViewMode;


            if (ImGui.Selectable(ViewModes[i], selected, ImGuiSelectableFlags.None,
                    new Vector2(SelectorWidth, GuiTheme.TopbarHeight)))
            {
                OnViewModeChanged((EditorViewMode)i);
            }
        }
        ImGui.EndChild();
    }

    private void DrawPropertySelector()
    {
        float x = ImGui.GetContentRegionAvail().X;
        float startPosX = x - (74/2f) - (GuiTheme.TopbarHeight * 5);
        ImGui.SameLine(startPosX);

        if (!ImGui.BeginChild("##editor-property-selector")) return;
        for (var i = 0; i < PropertyModes.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            var selected = i == (int)_ctx.PropertyMode;
            if (ImGui.Selectable(PropertyModes[i], selected, ImGuiSelectableFlags.None,
                    new Vector2(GuiTheme.TopbarHeight, GuiTheme.TopbarHeight)))
            {
                OnPropertyModeChange((RightSidebarMode)i);
            }
        }

        ImGui.EndChild();

    }
}