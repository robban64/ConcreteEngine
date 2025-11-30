#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Editor.Data;

public enum EditorMouseAction : byte
{
    None,
    SelectEntity,
    DragEntityOverTerrain,
    StopDragEntity
}

public struct EditorWorldMouseData
{
    public Vector2 MousePosition;
    public int EntityId;
    public EditorMouseAction Action;
}