using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGuizmo;

namespace ConcreteEngine.Editor.Utils;

internal static class EditorEnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once IdentifierTypo
    public static ImGuizmoOperation ToImGuizmo(this TransformGizmoOp op)
    {
        return op switch
        {
            TransformGizmoOp.Translate => ImGuizmoOperation.Translate,
            TransformGizmoOp.Rotate => ImGuizmoOperation.Rotate,
            TransformGizmoOp.Scale => ImGuizmoOperation.Scale,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }
}