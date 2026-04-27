using System.Numerics;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Theme;

internal static class WindowLayout
{
    public static void CalculateLayout(EditorWindowLayout left, EditorWindowLayout right, EditorWindowLayout bottom)
    {
        var vp = ImGui.GetMainViewport();
        const float width = GuiTheme.SidebarDefaultWidth;

        var height = vp.WorkSize.Y - GuiTheme.TopbarHeight;
        var size = new Vector2(width, height);
        left.Position = vp.WorkPos with { Y = vp.WorkPos.Y + GuiTheme.TopbarHeight };
        left.Size = size;
        right.Size = size;
        right.Position = new Vector2(vp.WorkPos.X + vp.WorkSize.X - width, vp.WorkPos.Y + GuiTheme.TopbarHeight);
        CalculateConsoleLayout(bottom, vp, size.X, size.X);
    }

    private static void CalculateConsoleLayout(EditorWindowLayout layout, ImGuiViewportPtr vp, float leftW,
        float rightW)
    {
        const float minW = 400f, maxWCap = 980f;
        const float minH = 240f, maxH = 300f;
        const float margin = 12f;

        var centerX = vp.WorkPos.X + leftW;
        var centerY = vp.WorkPos.Y;
        var centerW = float.Max(0, vp.WorkSize.X - leftW - rightW);
        var centerH = vp.WorkSize.Y;

        var targetW = float.Clamp(centerW * 0.80f, minW, float.Min(maxWCap, centerW));
        var targetH = float.Clamp(centerH * 0.25f, minH, maxH);

        var posX = centerX + float.Max(0, (centerW - targetW) * 0.5f);
        var posY = centerY + centerH - targetH - margin;

        layout.Position = new Vector2(posX, posY);
        layout.Size = new Vector2(targetW, targetH);
        layout.SizeMin = new Vector2(float.Min(minW, centerW), minH);
        layout.SizeMax = new Vector2(float.Min(float.Min(maxWCap, centerW), centerW), float.Min(maxH, centerH));
    }

/*
    private static void DrawLeftSidebarHeader(StateContext stateContext)
    {
        if (stateContext.IsMetricMode) return;

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, SidebarTabFramePadding);

        if (ImGui.BeginTabBar("##panel-tabs"u8, ImGuiTabBarFlags.FittingPolicyShrink))
        {
            var panelId = WindowManager.GetWindow(WindowId.Left).ActivePanel?.Id ?? PanelId.None;

            if (ImGui.BeginTabItem("Asset"u8))
            {
                if (panelId != PanelId.AssetList)
                    stateContext.EmitTransition(WindowId.Left, PanelId.AssetList);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Scene"u8))
            {
                if (panelId != PanelId.SceneList)
                    stateContext.EmitTransition(WindowId.Left, PanelId.SceneList);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar();
    }
*/
}