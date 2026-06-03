using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Assets;

internal static class AssetSystemSetup
{
    private static Stopwatch _loadTimer = new();
    private static long _allocStart;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Start()
    {
        _allocStart = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"Alloc Before loader: {_allocStart / 1000.0 / 1000.0}mb");
        _loadTimer.Start();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void End()
    {
        _loadTimer.Stop();
        var alloc = GC.GetAllocatedBytesForCurrentThread() - _allocStart;
        var str = $"Asset load time: {_loadTimer.ElapsedTicks / 1000.0 / 1000.0}, Alloc: {alloc / 1000.0 / 1000.0}mb\n";
        Console.Write(str);
        File.AppendAllText(EnginePath.LoadTimeFilePath, str);
        _loadTimer.Reset();
        _loadTimer = null!;

        CollectGc();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CollectGc()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CreateFallbackAssets(AssetStore assets, MaterialStore materials)
    {
        // Texture
        {
            var gid = Guid.Parse("196d3a4f-99e9-4d5a-971b-b42aa0012970");
            var assetId = assets.RegisterPlainAsset(gid, AssetKind.Texture, "White", AssetStorageKind.InMemory);
            assets.AddAsset(new Texture(
                "White",
                assetId,
                gid,
                GfxTextures.Fallback.AlbedoId,
                new Size2D(1),
                new TextureProperties(
                    lod: 0,
                    kind: TextureKind.Texture2D,
                    preset: TexturePreset.NearestClamp,
                    anisotropy: AnisotropyLevel.Off,
                    pixelFormat: TexturePixelFormat.Rgba
                ))
            );
        }

        // Material
        {
            var gid = Guid.Parse("f28fbc18-9e84-41bf-b490-4b900b1d8598");
            var assetId = assets.RegisterPlainAsset(gid, AssetKind.Material, "Fallback", AssetStorageKind.InMemory);
            var material = MaterialLoader.CreateFallback(assetId, gid);
            materials.AddFallbackMaterial(material);
            assets.AddAsset(material);
        }
    }
}