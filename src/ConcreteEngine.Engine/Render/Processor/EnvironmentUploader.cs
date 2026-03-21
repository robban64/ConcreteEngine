using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class EnvironmentUploader
{
    private static Matrix4x4 _terrainModelMat;
    private static Matrix3X4 _terrainNormalMat;

    private static Matrix4x4 _skyboxModelMat;
    private static Matrix3X4 _skyboxNormalMat;

    public static void RefreshMatrices()
    {
        CreateTransformMatrices(in Transform.Identity, out _terrainModelMat, out _terrainNormalMat);
        CreateTransformMatrices(in Transform.Identity, out _skyboxModelMat, out _skyboxNormalMat);
    }
    
    public static void SubmitDrawTerrain(DrawCommandBuffer commandBuffer, TerrainMeshGenerator terrain)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
        var cmd = new DrawCommand(terrain.MeshId, terrain.BoundMaterial);
        commandBuffer.SubmitDraw(cmd, meta, in _terrainModelMat, in _terrainNormalMat);
    }

    public static void SubmitDrawSkybox(DrawCommandBuffer commandBuffer, Skybox sky)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
        var cmd = new DrawCommand(sky.Mesh, sky.Material);
        commandBuffer.SubmitDraw(cmd, meta, in _skyboxModelMat, in _skyboxNormalMat);
    }

    
    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model,
        out Matrix3X4 normal)
    {
        MatrixMath.CreateModelMatrix(in transform, out model);
        MatrixMath.CreateNormalMatrix(in model, out normal);
    }
}