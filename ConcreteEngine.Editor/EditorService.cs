#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Gui;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Editor.ViewModel;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor;

internal static class EditorService
{
    private const long RefreshInterval = 1_500;

    private static long _lastRefresh = TimeUtils.GetTimestamp();

    private static EditorModeState ModeState => StateContext.ModeState;

    private static Vector2 _prevMousePos;

    private static bool _wasDragging = false;
    private static float _dragPlaneHeight;
    private static Vector3 _dragOffset;

    private static bool IsMouseOverEditor()
    {
        var io = ImGui.GetIO();
        if (io.WantCaptureMouse || ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows))
            return true;

        return false;
    }

    private static Vector3 GetRayPlaneIntersection(Ray ray, float planeY)
    {
        // Plane normal is Up (0,1,0)
        float denom = ray.Direction.Y;

        // Prevent divide by zero (ray parallel to plane)
        if (Math.Abs(denom) < 1e-6f) return Vector3.Zero;

        // t = (PlaneY - RayOriginY) / RayDirY
        float t = (planeY - ray.Position.Y) / denom;

        // Check if intersection is behind camera
        if (t < 0) return Vector3.Zero;

        return ray.Position + ray.Direction * t;
    }

    private static void UpdateEditorInput(float delta)
    {
        var mousePos = ImGui.GetMousePos();
        var mouseDelta = mousePos - _prevMousePos;
        var deltaAbs = Vector2.Abs(mouseDelta);

        var isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
        var isClick = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var isReleased = ImGui.IsMouseReleased(ImGuiMouseButton.Left);

        var entityModel = ModelManager.EntitiesState;
        var entityState = entityModel.State!;

        var selectedEntity = entityState.SelectedEntity;

        var request = new EditorWorldMouseData { MousePosition = mousePos, };

        if (isClick)
        {
            request.Action = EditorMouseAction.MouseSelectEntity;
            entityModel.TriggerEvent(EventKey.MouseSelectEntity, request);
            return;
        }

        if (isReleased)
        {
            
        }

        var hasDelta = deltaAbs.X > 0 || deltaAbs.Y > 0;
        if (isDragging)
        {
            request.Action = EditorMouseAction.MouseDragEntityTerrain;
            EditorApi.SendClickRequest(in request, out request);
        }

        _prevMousePos = mousePos;
    }

    internal static void Render(float delta, bool blockInput)
    {
        if (!blockInput && !IsMouseOverEditor()) UpdateEditorInput(delta);

        StateContext.CommitState();
        GuiTheme.RightSidebarExpanded = ModeState.IsEditorState;
        MetricsApi.ToggleMetrics(ModeState.IsMetricState);

        RefreshData();

        GuiTheme.PushTheme();

        if (!blockInput)
            CheckHotkeys();


        Topbar.Draw();
        if (!StateContext.ModeState.IsEmptyViewMode)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 10f));

            LeftSidebar.Draw(GuiTheme.LeftSidebarWidth, offset: GuiTheme.TopbarHeight);
            RightSidebar.Draw(GuiTheme.RightSidebarWidth, offset: GuiTheme.TopbarHeight);

            ImGui.PopStyleVar(2);
        }

        ConsoleService.Draw(GuiTheme.LeftSidebarWidth, GuiTheme.RightSidebarWidth);
    }

    private static void CheckHotkeys()
    {
        if (ImGui.IsKeyPressed(ImGuiKey._1)) StateContext.ToggleLeftSidebar(LeftSidebarMode.Assets);
        else if (ImGui.IsKeyPressed(ImGuiKey._2)) StateContext.ToggleLeftSidebar(LeftSidebarMode.Entities);
        else if (ImGui.IsKeyPressed(ImGuiKey._3)) StateContext.ToggleRightSidebar(RightSidebarMode.Camera);
        else if (ImGui.IsKeyPressed(ImGuiKey._4)) StateContext.ToggleRightSidebar(RightSidebarMode.World);
        else if (ImGui.IsKeyPressed(ImGuiKey._5)) StateContext.ToggleRightSidebar(RightSidebarMode.Sky);
        else if (ImGui.IsKeyPressed(ImGuiKey._6)) StateContext.ToggleRightSidebar(RightSidebarMode.Terrain);
    }

    private static void RefreshData()
    {
        if (!ModeState.IsEditorState) return;
        if (!TimeUtils.HasIntervalPassed(_lastRefresh, RefreshInterval)) return;

        if (ModeState.RightSidebar == RightSidebarMode.Camera && ModelManager.CameraState.State is not null)
        {
            ModelManager.CameraState.InvokeAction(TransitionKey.Refresh);
            _lastRefresh = TimeUtils.GetTimestamp();
        }

        ModelManager.InvokeRefreshForModels();
    }

/*
     private static long _lastAction = TimeUtils.GetTimestamp();
     private static long _lastFetched = TimeUtils.GetTimestamp();

     private static TResponse? OnApiFill<TRequest, TResponse>(TRequest request,
       ApiModelRequestDel<TRequest, TResponse>? apiFetch) where TRequest : class where TResponse : class
   {
       if (!CanFetch(150) || apiFetch is null) return null;
       Console.WriteLine($"Api Fill: {typeof(TResponse).Name}");
       return apiFetch(request);
   }

    private static bool CanFetch(int ms)
    {
        if (!TimeUtils.HasIntervalPassed(_lastFetched, ms)) return false;
        _lastFetched = TimeUtils.GetTimestamp();
        return true;
    }

    private static bool CanExecute(int ms)
    {
        if (!TimeUtils.HasIntervalPassed(_lastAction, ms))
        {
            ConsoleService.SendLog("Command delay time has not passed");
            return false;
        }

        _lastAction = TimeUtils.GetTimestamp();
        return true;
    }
    */
}