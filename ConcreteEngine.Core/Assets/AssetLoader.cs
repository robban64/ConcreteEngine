using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Factories;
using ConcreteEngine.Core.Assets.Manifest;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Assets;

//Wip
internal sealed class AssetLoader
{
    
    private AssetAssemblerRegistry? _assemblerRegistry;
    private AssetProcessor? _loader;
    private AssetGfxUploader? _uploader;
    
    public bool IsLoading { get; private set; } = false;

    internal void Start(GfxContext gfx,  AssetManifestBundle assetRecords)
    {
        IsLoading = true;
        _uploader = new AssetGfxUploader(gfx);
        _loader = new AssetProcessor(AssetPaths.AssetFolder, _uploader);
        _loader.Start(assetRecords);
    }

    internal bool ProcessLoader(int n, AssetSystem assetSystem)
    {
        ArgumentNullException.ThrowIfNull(assetSystem);
        InvalidOpThrower.ThrowIfNot(IsLoading);

        for (var i = 0; i < n; i++)
        {
            if (_loader!.Process(out var finalEntry)) return true;
            if (finalEntry is not null)
                _assemblerRegistry!.AssembleAsset(finalEntry, assetSystem);
        }

        return false;
    }
}