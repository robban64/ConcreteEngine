#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Scene.Entities;

internal static class EntityUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MakeDrawMesh(ModelComponent m, in Transform t, out DrawEntity drawEntity)
        => drawEntity = new DrawEntity(m, in t, DrawCommandId.Mesh, DrawCommandQueue.Opaque, PassMask.Default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MakeDrawTerrain(ModelComponent m, in Transform t, out DrawEntity drawEntity)
        => drawEntity = new DrawEntity(m, in t, DrawCommandId.Terrain, DrawCommandQueue.Terrain, PassMask.Default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MakeSkybox(ModelComponent m, in Transform t, out DrawEntity drawEntity)
        => drawEntity = new DrawEntity(m, in t, DrawCommandId.Skybox, DrawCommandQueue.Skybox, PassMask.Main);
}