using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics;
using static ConcreteEngine.Engine.Assets.Loader.ImporterModel.AssimpUtils;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterModel;

internal sealed class ModelImportContext
{
    public static ModelImportContext Instance { get; private set; } = null!;

    public static void CreateContext(MeshScratchpad scratchpad)
    {
        ArgumentNullException.ThrowIfNull(scratchpad);
        if (Instance is not null) throw new InvalidOperationException(nameof(Instance));
        Instance = new ModelImportContext(scratchpad);
    }

    public static void CloseContext()
    {
        if (Instance is null) throw new InvalidOperationException(nameof(Instance));
        Instance.Close();
        Instance = null!;
    }

    private ModelImportContext(MeshScratchpad meshScratchpad) {
        Scratchpad = meshScratchpad;
    }

    public readonly MeshScratchpad Scratchpad;

    public readonly Dictionary<string, int> BoneIndexByName = new(BoneLimit);
    public readonly Dictionary<(int meshIndex, int boneOrder), int> BoneIndexByMeshBone = new(BoneLimit);
    public readonly Dictionary<string, IntPtr> NodeMap = new(BoneLimit);

    public readonly List<EmbeddedSceneTexture> Textures = new(8);
    public readonly List<EmbeddedSceneMaterial> Materials = new(4);
    
    public ModelData Model = null!;
    public ModelAnimation? Animation;
    
    public string ModelName { get; private set; }
    public string Filename { get; private set; }
    
    public bool Active { get; private set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Being(string assetName, string filename, Span<(int vertexCount, int indexCount)> dataCount)
    {
        if (Active) throw new InvalidOperationException(nameof(Active));
        ModelName = assetName;
        Filename = filename;
        Scratchpad.Begin(dataCount);
        Active = true;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void End()
    {
        if (!Active) throw new InvalidOperationException(nameof(Active));
        Scratchpad.End();
        ClearCollections();
        ResetFields();
        Active = false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Close()
    {
        if (Active) throw new InvalidOperationException(nameof(Active));
        ClearCollections();
        ResetFields();
        
        NodeMap.TrimExcess();
        BoneIndexByName.TrimExcess();
        BoneIndexByMeshBone.TrimExcess();
        Textures.TrimExcess();
        Materials.TrimExcess();
        
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ClearCollections()
    {
        NodeMap.Clear();
        BoneIndexByName.Clear();
        BoneIndexByMeshBone.Clear();
        Textures.Clear();
        Materials.Clear();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ResetFields()
    {
        ModelName = null!;
        Filename = null!;
        Model = null!;
        Animation = null!;
    }

}
