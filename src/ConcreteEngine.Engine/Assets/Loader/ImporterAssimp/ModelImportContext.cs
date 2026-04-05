using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Loader.Data;

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
    }
}