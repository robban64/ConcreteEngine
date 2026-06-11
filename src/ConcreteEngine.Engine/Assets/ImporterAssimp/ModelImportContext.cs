using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Assets.Loader.Data;

namespace ConcreteEngine.Engine.Assets.ImporterAssimp;

internal sealed class ModelImportContext(
    string modelName,
    string filename,
    ModelImportData model,
    ModelRig? animation,
    int materialCount,
    int textureCount)
{
    public readonly List<EmbeddedSceneTexture> Textures = new(textureCount);
    public readonly List<EmbeddedSceneMaterial> Materials = new(materialCount);

    public ModelImportData Model = model;
    public ModelRig? Animation = animation;

    public string ModelName = modelName;
    public string Filename = filename;

    private TextureLoader? _textureLoader;
    
    public void SetTextureLoader(TextureLoader textureLoader) => _textureLoader = textureLoader;

    public unsafe void RegisterTexture(EmbeddedSceneTexture texture, byte* data, int length)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        if(_textureLoader == null) Throwers.NullReference(nameof(_textureLoader));
        _textureLoader.StoreEmbedded(texture.GId, data, length, texture.PixelFormat, out texture.Dimensions);
    }
    

    public void Clear()
    {
        Textures.Clear();
        Materials.Clear();

        ModelName = null!;
        Filename = null!;
        Model = null!;
        Animation = null!;
        _textureLoader = null;
    }
}