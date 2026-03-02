using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render.Processor;

internal static class WorldObjectProcessor
{
    internal static void SubmitWorldObjects(DrawCommandBuffer commandBuffer, WorldBundle renderCtx)
    {
        SubmitDrawTerrain(commandBuffer, renderCtx.Terrain);
        SubmitDrawSkybox(commandBuffer, renderCtx.Sky);
    }

    private static void SubmitDrawTerrain(DrawCommandBuffer commandBuffer, Terrain terrain)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
        var cmd = new DrawCommand(terrain.Mesh, terrain.Material);

        CreateTransformMatrices(Transform.Identity, out var model, out var normal);
        commandBuffer.SubmitDraw(cmd, meta, in model, in normal);
    }

    private static void SubmitDrawSkybox(DrawCommandBuffer commandBuffer, WorldSky sky)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
        var cmd = new DrawCommand(sky.Mesh, sky.Material);

        CreateTransformMatrices(Transform.Identity, out var model, out var normal);
        commandBuffer.SubmitDraw(cmd, meta, in model, in normal);
    }

    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model,
        out Matrix3X4 normal)
    {
        MatrixMath.CreateModelMatrix(in transform, out model);
        MatrixMath.CreateNormalMatrix(in model, out normal);
    }
}