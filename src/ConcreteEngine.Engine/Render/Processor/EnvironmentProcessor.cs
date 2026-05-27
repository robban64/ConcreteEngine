using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class EnvironmentProcessor
{
    private static DrawObjectUniform _terrainMatrixUniform;
    private static DrawObjectUniform _skyboxMatrixUniform;

    public static void SubmitDrawTerrain(DrawCommandBuffer commandBuffer, TerrainSystem terrain, CameraFrustum camera)
    {
        var mainTerrain = terrain.MainTerrain;
        var material = mainTerrain.MaterialId;
        var foliageMaterial = mainTerrain.FoliageMaterialId;

        ref readonly var transform = ref _terrainMatrixUniform;
        foreach (var it in terrain.TerrainMesh.GetMeshChunks())
        {
            if (!camera.IntersectsBox(in mainTerrain.GetChunk(it.Slot).GetBounds())) continue;
            var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
            var cmd = new DrawCommand(it.TerrainMeshId, material);
            commandBuffer.Submit(cmd, meta, in transform);

            if (it.FoliageCount > 0)
            {
                meta = new DrawCommandMeta(DrawCommandId.Particle, DrawCommandQueue.Transparent);
                cmd = new DrawCommand(it.FoliageMeshId, foliageMaterial, instanceCount: (uint)it.FoliageCount);
                commandBuffer.Submit(cmd, meta, in transform);
            }
        }
    }

    public static void SubmitDrawSkybox(DrawCommandBuffer commandBuffer, Skybox sky)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
        var cmd = new DrawCommand(sky.MeshId, sky.MaterialId);
        ref readonly var transform = ref _skyboxMatrixUniform;
        commandBuffer.Submit(cmd, meta, in transform);
    }

    public static void RefreshMatrices()
    {
        ref var sky = ref _skyboxMatrixUniform;
        ref var terrain = ref _terrainMatrixUniform;
        CreateTransformMatrices(in Transform.Identity, out sky.Model, ref sky.Normal);
        CreateTransformMatrices(in Transform.Identity, out terrain.Model, ref terrain.Normal);
    }

    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model,
        ref Matrix3X4 normal)
    {
        MatrixMath.CreateModelMatrix(in transform, out model);
        MatrixMath.CreateNormalMatrix(ref normal, in model);
    }
}