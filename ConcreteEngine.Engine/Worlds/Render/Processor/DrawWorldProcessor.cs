using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Worlds.Render.Processor;

internal static class DrawWorldProcessor
{
    internal static void SubmitWorldObjects(World world, DrawCommandUploader uploader)
    {
        SubmitDrawTerrain(uploader, world.MeshTableImpl, world.Terrain);
        SubmitDrawSkybox(uploader, world.Sky);
    }

    private static void SubmitDrawTerrain(DrawCommandUploader uploader, MeshTable meshTable, WorldTerrain terrain)
    {
        var view = meshTable.GetPartsRefView(terrain.Model);

        var meta = new DrawCommandMeta(DrawCommandId.Terrain, DrawCommandQueue.Terrain);
        var cmd = new DrawCommand(view.Parts[0].Mesh, terrain.Material);

        CreateTransformMatrices(in Transform.Identity, out var model, out var normal);
        uploader.SubmitDrawAndTransform(cmd, meta, in model, in normal);
    }

    private static void SubmitDrawSkybox(DrawCommandUploader uploader, WorldSkybox sky)
    {
        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
        var cmd = new DrawCommand(sky.Mesh, sky.Material);

        CreateTransformMatrices(in Transform.Identity, out var model, out var normal);
        uploader.SubmitDrawAndTransform(cmd, meta, in model, in normal);
    }

    private static void CreateTransformMatrices(in Transform transform, out Matrix4x4 model,
        out Matrix3X4 normal)
    {
        MatrixMath.CreateModelMatrix(
            in transform.Translation,
            in transform.Scale,
            in transform.Rotation,
            out model
        );

        MatrixMath.CreateNormalMatrix(in model, out normal);
    }
}