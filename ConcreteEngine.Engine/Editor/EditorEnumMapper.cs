using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Assets.Shaders;
using ConcreteEngine.Engine.Assets.Textures;

namespace ConcreteEngine.Engine.Editor;

internal static class EditorEnumMapper
{
    extension(EditorItemType itemType)
    {
        public Type ToAssetType()
        {
            return itemType switch
            {
                EditorItemType.Texture => typeof(Texture2D),
                EditorItemType.Shader => typeof(Shader),
                EditorItemType.Model => typeof(Model),
                EditorItemType.MaterialTemplate => typeof(MaterialTemplate),
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
        }

        public AssetKind ToAssetKind()
        {
            return itemType switch
            {
                EditorItemType.Texture => AssetKind.Texture2D,
                EditorItemType.Shader => AssetKind.Shader,
                EditorItemType.Model => AssetKind.Model,
                EditorItemType.MaterialTemplate => AssetKind.MaterialTemplate,
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
        }
    }

    extension(AssetKind itemType)
    {
        public EditorItemType ToEditorEnum()
        {
            return itemType switch
            {
                AssetKind.Texture2D => EditorItemType.Texture,
                AssetKind.TextureCubeMap => EditorItemType.Texture,
                AssetKind.Shader => EditorItemType.Shader,
                AssetKind.Model => EditorItemType.Model,
                AssetKind.MaterialTemplate => EditorItemType.MaterialTemplate,
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
        }

        public EditorAssetCategory ToEditorCategory()
        {
            return itemType switch
            {
                AssetKind.Texture2D => EditorAssetCategory.Texture,
                AssetKind.TextureCubeMap => EditorAssetCategory.Texture,
                AssetKind.Shader => EditorAssetCategory.Shader,
                AssetKind.Model => EditorAssetCategory.Model,
                AssetKind.MaterialTemplate => EditorAssetCategory.Material,
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
        }
    }
}