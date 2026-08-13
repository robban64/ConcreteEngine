using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal static partial class PassPipeline
{
    private static unsafe void SelectionRenderer(RenderPassProgram ctx)
    {
        TransformUniform* uniform = stackalloc TransformUniform[1];
        EditorEffectsUniform* effect = stackalloc EditorEffectsUniform[1];

        ctx.Gfx.UseShader(RenderRegistry.HighlightShader);
        RenderContext.OverrideShader = RenderRegistry.HighlightShader;

        foreach (var query in RenderEcs.Store<SelectionComponent>().VisibilityQuery())
        {
            var source = RenderEcs.Core.GetSource(query.Entity);

            *effect = new EditorEffectsUniform(source.IsSkinned(), query.Component.HighlightColor);

            uniform->Model = RenderEcs.Core.GetModelMatrix(query.Entity);
            uniform->Normal = Matrix3X4.Identity;

            ctx.GfxBuffers.UploadSingleUniform(effect, 0);
            ctx.GfxBuffers.UploadSingleUniform(uniform, 0);
            ctx.Gfx.BindUniformBufferRange<TransformUniform>(0, 1);

            if (source.IsSkinned()) ctx.DrawCmd.BindSkinningSlot(query.Entity);

            ctx.DrawCmd.BindMaterial(source.Material);

            ctx.Gfx.DrawMesh(source.Mesh);
        }
    }

    private static unsafe void DebugBoundsRenderer(RenderPassProgram ctx)
    {
        TransformUniform* uniform = stackalloc TransformUniform[1];
        EditorEffectsUniform* effect = stackalloc EditorEffectsUniform[1];
        RenderContext.OverrideShader = RenderRegistry.BoundingBoxShader;

        var materialId = AssetStore.Core.DebugBoundsMaterial.MaterialId;
        ctx.Gfx.UseShader(RenderRegistry.BoundingBoxShader);

        foreach (var query in RenderEcs.Store<DebugBoundsComponent>().VisibilityQuery())
        {
            var isSkinned = RenderEcs.Core.GetSource(query.Entity).IsSkinned();
            *effect = new EditorEffectsUniform(isSkinned, query.Component.Color);

            ref readonly var wb = ref RenderEcs.Core.GetWorldBounds(query.Entity);
            MatrixMath.CreateModelMatrix(wb.Center, wb.Extent, Quaternion.Identity, out uniform->Model);
            uniform->Normal = Matrix3X4.Identity;

            ctx.GfxBuffers.UploadSingleUniform(effect, 0);
            ctx.GfxBuffers.UploadSingleUniform(uniform, 0);
            ctx.Gfx.BindUniformBufferRange<TransformUniform>(0, 1);

            ctx.DrawCmd.BindMaterial(materialId);
            ctx.Gfx.DrawMesh(GfxMeshes.Cube);
        }
    }
}