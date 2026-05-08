using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Handles;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.UI;

internal static unsafe class ViewportWindow
{
    private const ImGuiWindowFlags ViewportFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse |
        ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavInputs;

    private static TexturePtrHandle _viewportTexHandle = TexturePtrHandle.Null;

    public static void Draw(StateManager state)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin(WindowRoot.ViewportWindowId, ViewportFlags);

        var size = ImGui.GetContentRegionAvail();

        state.GetOrSetTextureHandle(ImGuiSystem.OutputTexture, ref _viewportTexHandle);
        ImGui.Image(_viewportTexHandle, size, new Vector2(0, 1), new Vector2(1, 0));

        if (SelectionManager.Instance.SelectedSceneObject is { } inspector)
        {
            DrawGizmos(state, inspector);
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }


    [SkipLocalsInit]
    private static void DrawGizmos(StateManager state, InspectSceneObject inspector)
    {
        var tool = state.Context.Tool;
        
        var enabled = !EditorInput.IsGizmoBlocked;

        var size = WindowRoot.ViewportSize;
        var pos = WindowRoot.WorkPosition;

        ImGuizmo.BeginFrame();
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.SetDrawlist();
        ImGuizmo.SetRect(pos.X, pos.Y, size.X, size.Y);
        ImGuizmo.Enable(enabled);

        Matrix4x4* matrices = stackalloc Matrix4x4[3];
        var view = &matrices[0];
        var proj = &matrices[1];
        var model = &matrices[2];

        *view = EngineObjectStore.Camera.ViewMatrix;
        *proj = EngineObjectStore.Camera.ProjectionMatrix;
        MatrixMath.CreateModelMatrix(in inspector.Transform.GetTransform(), out *model);

        var changed = ImGuizmo.Manipulate(
            &view->M11,
            &proj->M11,
            tool.GizmoOp.ToImGuizmo(),
            tool.IsWorldGizmo ? ImGuizmoMode.World : ImGuizmoMode.Local,
            &model->M11
        );

        if (changed && enabled)
        {
            Transform.FromMatrix(in *model, out var transform);
            inspector.Transform.SetTransform(in transform);
        }
    }
}