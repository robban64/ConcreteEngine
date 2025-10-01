#region

using ConcreteEngine.Core.Assets.Manifest;

#endregion

namespace ConcreteEngine.Core.Assets.Factories;

internal sealed class AssetAssemblerRegistry
{
    private readonly Dictionary<AssetKind, IAssetAssembler> _assemblers = new(4);

    public AssetAssemblerRegistry()
    {
        RegisterAssembler(new TextureAssembler());
        RegisterAssembler(new CubeMapAssembler());
        RegisterAssembler(new ShaderAssembler());
        RegisterAssembler(new MeshAssembler());
    }

    public void AssembleAsset(IAssetFinalEntry finalEntry, AssetSystem assetSystem)
    {
        if (finalEntry.ProcessInfo.Status != AssetProcessStatus.Done)
            throw new InvalidOperationException($"Invalid status {nameof(finalEntry.ProcessInfo.Status)}.");

        if (!_assemblers.TryGetValue(finalEntry.ProcessInfo.AssetType, out var asm))
            throw new NotSupportedException($"No assembler for {finalEntry.ProcessInfo.AssetType}");

        asm.Assemble(finalEntry, assetSystem);
    }

    private void RegisterAssembler(IAssetAssembler assembler)
    {
        if (!_assemblers.TryAdd(assembler.Kind, assembler))
            throw new InvalidOperationException($"Duplicate assembler with kind {assembler.Kind}");
    }
}