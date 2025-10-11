#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Manifest;

internal interface IAssetFinalEntry
{
    AssetProcessInfo ProcessInfo { get; }
}

internal sealed record AssetFinalEntry<TRecord, TDescriptor, TGfxId>(
    TRecord Record,
    in TDescriptor Descriptor,
    TGfxId ResourceId,
    AssetProcessInfo ProcessInfo
) : IAssetFinalEntry
    where TRecord : class, IAssetManifestRecord
    where TDescriptor : struct
    where TGfxId : unmanaged, IResourceId;

internal record struct MeshCreationInfo(int DrawCount);

internal record struct TextureCreationInfo(
    int Width,
    int Height,
    TexturePixelFormat PixelFormat,
    byte[]? Data = null);

internal record struct CubeMapCreationInfo(int Width, int Height, TexturePixelFormat PixelFormat);

internal record struct ShaderCreationInfo(int Samplers);