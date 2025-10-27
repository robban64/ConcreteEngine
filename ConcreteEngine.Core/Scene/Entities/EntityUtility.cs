#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Core.Scene.Entities;

internal static class EntityUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MakeDrawMesh(in ModelComponent m, in Transform t, out DrawEntity drawEntity)
        => drawEntity = new DrawEntity(in m, in t, DrawCommandId.Mesh, DrawCommandQueue.Opaque, PassMask.Default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MakeDrawTerrain(in ModelComponent m, in Transform t, out DrawEntity drawEntity)
        => drawEntity = new DrawEntity(in m, in t, DrawCommandId.Terrain, DrawCommandQueue.Terrain, PassMask.Default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MakeSkybox(in ModelComponent m, in Transform t, out DrawEntity drawEntity)
        => drawEntity = new DrawEntity(in m, in t, DrawCommandId.Skybox, DrawCommandQueue.Skybox, PassMask.Main);
}