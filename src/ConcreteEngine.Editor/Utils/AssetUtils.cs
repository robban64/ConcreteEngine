using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Editor.Utils;

internal static class AssetUtils
{
    public static string GetAssetResourceIdName(IAsset asset, out int value)
    {
        switch (asset)
        {
            case IShader shader:
                value = shader.GfxId;
                return nameof(ShaderId);
            case IMaterial mat:
                value = mat.AssetShader;
                return nameof(ShaderId);
            case ITexture texture:
                value = texture.GfxId;
                return nameof(TextureId);
            case IModel model:
                value = model.Id;
                return nameof(ModelId);
            default:
                throw new ArgumentOutOfRangeException(nameof(asset), asset, null);
        }
    }
}