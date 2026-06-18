using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed partial class AssetStore
{
    public static int StoreCount => EnumCache<AssetKind>.Count - 1;

    private static class TypeStore<T> where T : AssetObject
    {
        public static readonly AssetTypeStore Store = new(AssetKindUtils.ToAssetKind(typeof(T)));
    }

    private static readonly Func<string, Type, bool> NameExistsDel =
        static (name, type) => !AssetManager.AssetStore.GetTypeStore(AssetKindUtils.ToAssetKind(type)).HasName(name);


    public static class Core
    {
        public static Shader FallbackShader { get; private set; } = null!;
        public static Material FallbackMaterial { get; private set; } = null!;
        public static Material DebugBoundsMaterial { get; private set; } = null!;

        internal static void SetupShaders(AssetStore store)
        {
            FallbackShader = store.GetByName<Shader>("Model");
        }
        internal static void CreateMaterials(AssetManager manager)
        {
            
            {
                var gid = Guid.Parse("f28fbc18-9e84-41bf-b490-4b900b1d8598");
                var assetId = manager.RegisterPlainAsset(gid, AssetKind.Material, "Fallback", AssetStorage.InMemory);
                var material= new Material("Fallback", assetId, gid, MaterialProfileId.Opaque);
                manager.Store.AddAsset(material);
                FallbackMaterial = material;
            }

            {
                var gid = Guid.Parse("747a4dcf-e9b0-47cd-a5b9-6a5b34ae40d6");
                var assetId = manager.RegisterPlainAsset(gid, AssetKind.Material, "DebugBounds", AssetStorage.InMemory);
                var material= new Material("DebugBounds", assetId, gid, MaterialProfileId.Opaque);
                material.State.DrawState =
                    GfxDrawState.Set(GfxDrawFlags.Blend, GfxDrawFlags.DepthWrite | GfxDrawFlags.Ac2);
                material.State.DrawFunctions = new GfxDrawFunctions(BlendMode.Alpha);
                
                manager.Store.AddAsset(material);
                
                DebugBoundsMaterial = material;
            }


        }
    }
}