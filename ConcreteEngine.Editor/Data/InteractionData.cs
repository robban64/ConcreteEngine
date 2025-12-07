#region

using System.Numerics;
using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.Data;


public struct EditorWorldMouseData
{
    public Vector2 MousePosition;
    public int EntityId;
    public EditorMouseAction Action;
}