using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Primitives;
using static ConcreteEngine.Engine.Assets.Loader.AssimpImporter.AssimpUtils;

namespace ConcreteEngine.Engine.Assets.Loader.ModelImporterV2;

internal sealed class AssimpSceneContext
{
    public static AssimpSceneContext Instance { get; private set; }

    public static void CreateContext(MeshScratchpad scratchpad)
    {
        ArgumentNullException.ThrowIfNull(scratchpad);
        if (Instance is not null) throw new InvalidOperationException(nameof(Instance));
        Instance = new AssimpSceneContext(scratchpad);
    }

    private AssimpSceneContext(MeshScratchpad meshScratchpad) {
        Scratchpad = meshScratchpad;
    }

    public readonly MeshScratchpad Scratchpad;
    
    public readonly Dictionary<int, string> BoneNameByIndex = new(BoneLimit);
    public readonly Dictionary<int, int> BoneParentByIndex = new(BoneLimit);
    public readonly Dictionary<(int meshIndex, int boneOrder), int> BoneIndexByMeshBone = new(BoneLimit);

    public ModelData Model = null!;
    public Animation Animation = null!;

}
/*
internal sealed class MeshScratchpad
{
    public uint[] Indices = new uint[IndicesCapacity];
    public Vertex3D[] Vertices = new Vertex3D[VertexCapacity];
    public VertexSkinned[] VerticesSkinned = new VertexSkinned[VertexCapacity];
    public SkinningData[] SkinningData = new SkinningData[VertexCapacity];


    public void EnsureCapacity(int? indexCount = null, int? vertexCount = null, int? skinnedCount = null)
    {
        if (indexCount is { } iCount && (uint)indexCount > Indices.Length)
        {
            var cap = Arrays.CapacityGrowthAlign(int.Max(iCount, 64));
            Array.Resize(ref Indices, cap);
            Console.WriteLine("triggered indicies " + cap);
        }

        if (vertexCount is { } vCount && (uint)vCount > Vertices.Length)
        {
            var cap = Arrays.CapacityGrowthPow2(int.Max(vCount, 64));
            Array.Resize(ref Vertices, cap);
            Console.WriteLine("triggered verts " + cap);
        }

        if (skinnedCount is { } skinCount && (uint)skinCount > VerticesSkinned.Length)
        {
            InvalidOpThrower.ThrowIf(SkinningData.Length != VerticesSkinned.Length);
            var cap = Arrays.CapacityGrowthPow2(int.Max(skinCount, 64));
            Array.Resize(ref VerticesSkinned, cap);
            Array.Resize(ref SkinningData, cap);
            Console.WriteLine("triggered skinn " + cap);
        }
    }
}*/