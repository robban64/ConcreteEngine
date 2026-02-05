using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Assets.Loader.Data;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;

internal sealed class ModelImportContext(string modelName, string filename, int materialCount, int textureCount)
{
    public readonly List<EmbeddedSceneTexture> Textures = new(textureCount);
    public readonly List<EmbeddedSceneMaterial> Materials = new(materialCount);
    
    public ModelImportData Model = null!;
    public ModelAnimation? Animation;

    public string ModelName = modelName;
    public string Filename = filename;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Clear()
    {
        Textures.Clear();
        Materials.Clear();
        Textures.TrimExcess();
        Materials.TrimExcess();

        ModelName = null!;
        Filename = null!;
        Model = null!;
        Animation = null!;
    }
}
