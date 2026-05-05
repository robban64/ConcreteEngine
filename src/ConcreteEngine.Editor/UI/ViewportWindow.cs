using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.UI;

internal static unsafe class ViewportWindow
{
    public static Vector2 Size;
    public static Vector2 Position;

    public static void Draw(StateManager state)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.SetNextWindowPos(Position);
        ImGui.SetNextWindowSize(Size);
        if (ImGui.Begin("##Viewport"u8, GuiTheme.ViewportFlags))
            DrawInner(state);

        ImGui.End();
        ImGui.PopStyleVar(1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawInner(StateManager state)
    {
        if (!state.TryGetTextureRefPtr(ImGuiSystem.OutputTexture, out var texPtr))
            throw new InvalidOperationException("Invalid viewport texture");

        ImGui.Image(*texPtr.Handle, Size, new Vector2(0, 1), new Vector2(1, 0));

        if (SelectionManager.Instance.SelectedSceneObject is { } inspector)
        {
            DrawGizmos(state.Context.Tool, inspector);
        }
    }

    [SkipLocalsInit]
    private static void DrawGizmos(ToolContext tool, InspectSceneObject inspector)
    {
        var enabled = !EditorInput.IsGizmoBlocked;
        ImGuizmo.SetDrawlist();
        ImGuizmo.SetRect(Position.X, Position.Y, Size.X, Size.Y);
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