using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Metadata.Asset;

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
    }

    extension(AssetKind itemType)
    {
        public EditorItemType ToEditorEnum()
        {
            return itemType switch
            {
                AssetKind.Texture => EditorItemType.Texture,
                AssetKind.Shader => EditorItemType.Shader,
                AssetKind.Model => EditorItemType.Model,
                AssetKind.Material => EditorItemType.MaterialTemplate,
                _ => throw new ArgumentOutOfRangeException(nameof(itemType), itemType, null)
            };
        }
    }
}