using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

[StructLayout(LayoutKind.Auto)]
public readonly record struct TextureSource(
    AssetId AssetId,
    TextureId TextureId,
    TextureId FallbackTexture,
    SamplerProfile Profile,
    TextureUsage Usage
)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFallback() => !AssetId.IsValid();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TextureId GetTextureOrFallback()
    {
        if (TextureId.IsValid()) return TextureId;
        return FallbackTexture.IsValid() ? FallbackTexture : GfxTextures.Fallback.AlbedoId;
    }
}