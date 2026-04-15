using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;

internal sealed class ModelImportContext(
    string modelName,
    string filename,
    ModelImportData model,
    ModelAnimation? animation,
    int materialCount,
    int textureCount)
{
    public readonly List<EmbeddedSceneTexture> Textures = new(textureCount);
    public readonly List<EmbeddedSceneMaterial> Materials = new(materialCount);

    public ModelImportData Model = model;
    public ModelAnimation? Animation = animation;

    public string ModelName = modelName;
    public string Filename = filename;

    private TextureLoader _textureLoader = null!;

    public void SetTextureLoader(TextureLoader textureLoader) => _textureLoader = textureLoader;

    public unsafe ArenaBlockPtr RegisterTexture(byte* data, int length, TexturePixelFormat format, out Size2D size)
    {
        return _textureLoader.StoreEmbedded(data, length, format, out size);
    }

    public void SanitizeClips()
    {
        if (Animation == null) return;

        foreach (var clip in Animation.Clips)
        {
            for (var i = 0; i < clip.Channels.Length; i++)
            {
                if (clip.Channels[i] == null!) clip.Channels[i] = new AnimationChannel(0, 0);
            }
        }
    }


    public void Clear()
    {
        Textures.Clear();
        Materials.Clear();

        ModelName = null!;
        Filename = null!;
        Model = null!;
        Animation = null!;
        _textureLoader = null!;
    }
}