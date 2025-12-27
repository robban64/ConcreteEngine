using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class WorldObjectProcessor
{
    internal static void SubmitWorldObjects(DrawCommandBuffer commandBuffer, World world)
    {
        SubmitDrawTerrain(commandBuffer, world.MeshTableImpl, world.Terrain);
        SubmitDrawSkybox(commandBuffer, world.Sky);
    }

    private static void SubmitDrawTerrain(DrawCommandBuffer commandBuffer, MeshTable meshTable, Terrain terrain)
    {
        var view = meshTable.GetModelPartView(terrain.Model);

        var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
        var cmd = new DrawCommand(view.Parts[0].Mesh, terrain.Material);

        CreateTransformMatrices(RenderTransform.Identity.Transform, out var model, out var normal);
        commandBuffer.SubmitDraw(cmd, meta, in model, in normal);
    }

    private static void SubmitDrawSkybox(DrawCommandBuffer commandBuffer, WorldSky sky)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
        var cmd = new DrawCommand(sky.Mesh, sky.Material);

        CreateTransformMatrices(RenderTransform.Identity.Transform, out var model, out var normal);
        commandBuffer.SubmitDraw(cmd, meta, in model, in normal);
    }

    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model,
        out Matrix3X4 normal)
    {
        MatrixMath.CreateModelMatrix(in transform, out model);
        MatrixMath.CreateNormalMatrix(in model, out normal);
    }
}