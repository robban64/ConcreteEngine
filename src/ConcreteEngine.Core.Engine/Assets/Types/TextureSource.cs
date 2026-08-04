using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

[StructLayout(LayoutKind.Auto)]
public readonly record struct TextureSource(
    AssetId AssetTexture,
    SamplerProfile Profile,
    TextureUsage Usage,
    TextureId FallbackTexture,
    TextureId OverrideTexture = default
)
{
    public bool IsBound() => AssetTexture.IsValid() || OverrideTexture.IsValid();

    public TextureSource WithTexture(AssetId assetTexture, SamplerProfile profile, TextureId overrideTexture = default) =>
        this with { AssetTexture = assetTexture, Profile = profile, OverrideTexture = overrideTexture };

    public TextureSource WithAssetId(AssetId assetId) => this with { AssetTexture = assetId };
}