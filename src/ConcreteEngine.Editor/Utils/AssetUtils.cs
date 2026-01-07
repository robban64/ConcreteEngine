using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Editor.Utils;

internal static class AssetUtils
{
    public static string GetAssetResourceIdName(AssetObject asset, out int value)
    {
        switch (asset)
        {
            case Shader shader:
                value = shader.ShaderId;
                return nameof(ShaderId);
            case MaterialTemplate mat:
                value = mat.AssetShader;
                return nameof(ShaderId);
            case Texture2D texture:
                value = texture.ResourceId;
                return nameof(TextureId);
            case Model model:
                value = model.ModelId;
                return nameof(ModelId);
            default:
                throw new ArgumentOutOfRangeException(nameof(asset), asset, null);
        }
    }
}