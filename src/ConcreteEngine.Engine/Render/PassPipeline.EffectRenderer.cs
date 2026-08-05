using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.RenderEntity.RenderComponent;
using ConcreteEngine.Engine.Render.Passes;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Render;

internal static partial class PassPipeline
{
    private static unsafe void SelectionRenderer(RenderPassContext ctx)
    {
        
        DrawObjectUniform* uniform = stackalloc DrawObjectUniform[1];
        EditorEffectsUniform* effect = stackalloc EditorEffectsUniform[1];
        
        ctx.Cmd.UseShader(RenderRegistry.HighlightShader);
        RenderContext.OverrideShader = RenderRegistry.HighlightShader;

        foreach (var query in RenderEcs.Store<SelectionComponent>().VisibilityQuery())
        {
            var source = RenderEcs.Core.GetSource(query.Entity);

            *effect = new EditorEffectsUniform(source.IsSkinned(), query.Component.HighlightColor);

            uniform->Model = RenderEcs.Core.GetModelMatrix(query.Entity);
            uniform->Normal = Matrix3X4.Identity;

            ctx.Buffers.UploadSingleUniform(effect, 0);
            ctx.Buffers.UploadSingleUniform(uniform, 0);
            ctx.Cmd.BindUniformBufferRange<DrawObjectUniform>(0, 1);

            if (source.IsSkinned()) ctx.DrawCmdProcessor.BindSkinningSlot(query.Entity);

            ctx.DrawCmdProcessor.BindMaterial(source.Material);

            ctx.Cmd.DrawMesh(source.Mesh);
        }
    }

    private static unsafe void DebugBoundsRenderer(RenderPassContext ctx)
    {
        DrawObjectUniform* uniform = stackalloc DrawObjectUniform[1];
        EditorEffectsUniform* effect = stackalloc EditorEffectsUniform[1];
        RenderContext.OverrideShader = RenderRegistry.BoundingBoxShader;

        var materialId = AssetStore.Core.DebugBoundsMaterial.MaterialId;
        ctx.Cmd.UseShader(RenderRegistry.BoundingBoxShader);

        foreach (var query in RenderEcs.Store<DebugBoundsComponent>().VisibilityQuery())
        {
            var isSkinned = RenderEcs.Core.GetSource(query.Entity).IsSkinned();
            *effect = new EditorEffectsUniform(isSkinned, query.Component.Color);

            ref readonly var wb = ref RenderEcs.Core.GetWorldBounds(query.Entity);
            MatrixMath.CreateModelMatrix(wb.Center, wb.Extent, Quaternion.Identity, out uniform->Model);
            uniform->Normal = Matrix3X4.Identity;
            
            ctx.Buffers.UploadSingleUniform(effect, 0);
            ctx.Buffers.UploadSingleUniform(uniform, 0);
            ctx.Cmd.BindUniformBufferRange<DrawObjectUniform>(0, 1);

            ctx.DrawCmdProcessor.BindMaterial(materialId);
            ctx.Cmd.DrawMesh(GfxMeshes.Cube);
        }
    }
}