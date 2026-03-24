using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class EnvironmentUploader
{
    private static DrawObjectUniform _terrainMatrixUniform;
    private static DrawObjectUniform _skyboxMatrixUniform;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SubmitDrawTerrain(DrawCommandBuffer commandBuffer, TerrainManager terrain)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
        var cmd = new DrawCommand(terrain.Terrain.MeshId, terrain.Terrain.MaterialId);
        ref readonly var transform = ref _terrainMatrixUniform;
        commandBuffer.Submit(cmd, meta, in transform);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SubmitDrawSkybox(DrawCommandBuffer commandBuffer, Skybox sky)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
        var cmd = new DrawCommand(sky.MeshId, sky.MaterialId);
        ref readonly var transform = ref _skyboxMatrixUniform;
        commandBuffer.Submit(cmd, meta, in transform);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RefreshMatrices()
    {
        ref var sky = ref _skyboxMatrixUniform;
        ref var terrain = ref _terrainMatrixUniform;
        CreateTransformMatrices(in Transform.Identity, out sky.Model, out sky.Normal);
        CreateTransformMatrices(in Transform.Identity, out terrain.Model, out terrain.Normal);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model,
        out Matrix3X4 normal)
    {
        MatrixMath.CreateModelMatrix(in transform, out model);
        MatrixMath.CreateNormalMatrix(in model, out normal);
    }
}