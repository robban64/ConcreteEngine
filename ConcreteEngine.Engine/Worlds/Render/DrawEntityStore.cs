#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.TransformData;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal static class DrawEntityStore
{
    public const int DefaultCapacity = 512;
    public const int MaxCapacity = 1024 * 10;
    
    //...
    public static int[] ByEntityId = new int[DefaultCapacity];
    public static DrawEntity[] Entities = new DrawEntity[DefaultCapacity];
    public static DrawEntityData[] EntityData = new DrawEntityData[DefaultCapacity];
    //...

    public static ref DrawEntity GetEntityById(EntityId entityId) => ref Entities[ByEntityId[entityId]];

    public static void GetDrawArrays(out DrawEntity[] entities, out DrawEntityData[] entityData, out int[] byEntityId)
    {
        entities = Entities;
        entityData = EntityData;
        byEntityId = ByEntityId;
    }

    public static void EnsureDrawEntityData(int amount)
    {
        InvalidOpThrower.ThrowIf(ByEntityId.Length != Entities.Length);
        InvalidOpThrower.ThrowIf(ByEntityId.Length != EntityData.Length);

        if (Entities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(Entities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        Array.Resize(ref Entities, newCap);
        Array.Resize(ref EntityData, newCap);
        Array.Resize(ref ByEntityId, newCap);
        Console.WriteLine($"Entity buffer resize: {newCap}");
    }
}

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
        public static WorldEntities WorldEntities = null!;
    }

    internal static void EnsureBuffer(int entityCap, int skinningCap)
    {
        ManagedStorage.CmdBuffer.EnsureBufferCapacity(entityCap);
        ManagedStorage.CmdBuffer.EnsureBoneBuffer(entityCap);
    }

    internal static DrawCommandUploader GetDrawUploaderCtx() 
        => ManagedStorage.CmdBuffer.GetDrawUploaderCtx();

    internal static SkinningBufferUploader GetSkinningUploaderCtx() =>
        ManagedStorage.CmdBuffer.GetSkinningUploaderCtx();

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
        ManagedStorage.WorldEntities = worldEntities;
    }
}