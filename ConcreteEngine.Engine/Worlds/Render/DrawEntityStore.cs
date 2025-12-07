#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.World;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal static class DrawDataProvider
{
    //....
    public static RenderFrameInfo FrameInfo;
    public static ProjectionInfoData ProjectionInfo;
    public static RenderViewSnapshot ViewData;
    //....

    public static float DeltaTime => FrameInfo.DeltaTime;
    
    private static class ManagedStorage
    {
        public static DrawCommandBuffer CmdBuffer = null!;
        public static AnimationTable AnimationTable = null!;
        public static MeshTable MeshTable = null!;
        public static MaterialTable MaterialTable = null!;
    }

    internal static void EnsureBuffer(int entityCap, int skinningCap)
    {
        ManagedStorage.CmdBuffer.EnsureBufferCapacity(entityCap);
        ManagedStorage.CmdBuffer.EnsureBoneBuffer(entityCap);
    }
    
    internal static WorldEntities WorldEntities = null!;

    internal static DrawCommandUploader GetDrawUploaderCtx() 
        => ManagedStorage.CmdBuffer.GetDrawUploaderCtx();

    internal static SkinningBufferUploader GetSkinningUploaderCtx() =>
        ManagedStorage.CmdBuffer.GetSkinningUploaderCtx();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ResolveMaterial(MaterialTagKey key, out MaterialTag tag) =>
        ManagedStorage.MaterialTable.ResolveSubmitMaterial(key, out tag);

    internal static AnimationDataView GetAnimationDataView() 
        => ManagedStorage.AnimationTable.GetDataView();

    internal static ReadOnlySpan<Matrix4x4> GetPartTransforms(ModelId id) =>
        ManagedStorage.MeshTable.GetPartTransforms(id);

    internal static ReadOnlySpan<MeshPart> GetMeshParts(ModelId id) 
        => ManagedStorage.MeshTable.GetMeshParts(id);
    
    internal static ModelPartView GetPartsRefView(ModelId id) 
        => ManagedStorage.MeshTable.GetPartsRefView(id);

    
    internal static void Attach(DrawCommandBuffer cmdBuffer, AnimationTable animationTable, MeshTable meshTable,
        MaterialTable materialTable,
        WorldEntities worldEntities)
    {
        ManagedStorage.CmdBuffer = cmdBuffer;
        ManagedStorage.AnimationTable = animationTable;
        ManagedStorage.MeshTable = meshTable;
        ManagedStorage.MaterialTable = materialTable;
        WorldEntities = worldEntities;
    }
}