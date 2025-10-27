#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Core.RenderingSystem.Data;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Scene.Entities;

internal static class EntityUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MakeDrawMesh(EntityId ent, ModelId model, int draw, in Transform t, out DrawEntity drawEntity)
        => drawEntity = new DrawEntity(ent, model, draw, in t, DrawCommandId.Mesh, DrawCommandQueue.Opaque,
            PassMask.Default);

}