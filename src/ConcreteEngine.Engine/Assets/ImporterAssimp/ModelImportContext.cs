using System.Collections.ObjectModel;
using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Assets.ImporterAssimp;

using static AssimpUtils;

internal sealed class ModelImportContext(TextureLoader textureLoader)
{
    public string? ModelName;
    public string? Filename;

    public bool IsAnimated;

    public int HashIndex;
    public readonly uint[] Hashes = new uint[BoneLimit * 2];
    public readonly IntPtr[] Nodes = new IntPtr[BoneLimit * 2];
    public readonly short[] BoneIndices = new short[BoneLimit * 2];
    public readonly Dictionary<string, int> BoneIndexByName = new(BoneLimit);

    public readonly MeshImportContext MeshContext = new();
    public readonly AnimationImportContext AnimationContext = new();
    public readonly EmbeddedImportContext EmbeddedContext = new(textureLoader);

    
    public bool TryGetBoneIndex(uint hash, out int boneIndex)
    {
        var idx = Hashes.IndexOf(hash);
        boneIndex = idx >= 0 ? BoneIndices[idx] : -1;
        return boneIndex >= 0;
    }

    public bool TryGetNode(uint hash, out IntPtr nodePtr)
    {
        var idx = Hashes.IndexOf(hash);
        nodePtr = idx >= 0 ? Nodes[idx] : -1;
        return nodePtr > 0;
    }

    public void StartContext(string modelName, string filename)
    {
        if(HashIndex > 0 || BoneIndexByName.Count > 0 || Filename != null || ModelName != null)
            Throwers.InvalidOperation("Context already initialized");
        ModelName = modelName;
        Filename = filename;

    }

    public void Begin(AssimpSceneMeta meta, bool isAnimated)
    {
        IsAnimated = isAnimated;
        MeshContext.Begin(meta);
        AnimationContext.Begin(meta);
        EmbeddedContext.Begin(meta);
    }
    
    public void Reset()
    {
        ModelName = null;
        Filename = null;
        HashIndex = 0;
        IsAnimated = false;

        Array.Clear(Hashes);
        Array.Clear(Nodes);
        BoneIndices.AsSpan().Fill(-2);
        BoneIndexByName.Clear();
        MeshContext.Reset();
        AnimationContext.Reset();
        EmbeddedContext.Reset();
    }

    public ModelInfo Compile(List<IEmbeddedAsset> embeddedSink, out ReadOnlySpan<Core.Engine.Graphics.Mesh> meshes,out ModelRig? rig)
    {
        rig = IsAnimated ? AnimationContext.Compile(BoneIndexByName) : null;
        meshes = MeshContext.Compile();
        
        EmbeddedContext.Compile(embeddedSink);

        return new ModelInfo(
            vertexCount: MeshContext.TotalVertexCount,
            faceCount: MeshContext.TotalFaceCount,
            boneCount: (ushort)(AnimationContext.BoneCount),
            meshCount: (byte)MeshContext.MeshCount,
            materialCount: (byte)EmbeddedContext.MaterialCount,
            textureCount: (byte)EmbeddedContext.TextureCount,
            isAnimated: IsAnimated
        );
    }
    
}

internal sealed class MeshImportContext
{
    public int MeshCount;
    public int TotalVertexCount;
    public int TotalFaceCount;
    
    public BoundingBox ModelBounds;

    public Core.Engine.Graphics.Mesh[] Meshes = new Core.Engine.Graphics.Mesh[16];
    public MemoryBlockPtr[] MeshMemory = new MemoryBlockPtr[16];

    public ReadOnlySpan<Core.Engine.Graphics.Mesh> Compile()
    {
        if(MeshCount == 0) Throwers.InvalidOperation(nameof(MeshCount));
        return Meshes.AsSpan(0, MeshCount);
    }

    public void Begin(AssimpSceneMeta meta)
    {
        ArgumentOutOfRangeException.ThrowIfZero(meta.MeshCount, nameof(meta.MeshCount));
        if(MeshCount > 0 || TotalVertexCount > 0 || TotalFaceCount > 0)
            Throwers.InvalidOperation("Context already initialized");
        
        MeshCount = meta.MeshCount;
        if (meta.MeshCount > Meshes.Length)
        {
            int length = int.Max(meta.MeshCount, Meshes.Length * 2);
            Array.Resize(ref Meshes, length);
            Array.Resize(ref MeshMemory, length);

        }
    }
    
    public void Reset()
    {
        ModelBounds = default;
        MeshCount = 0;
        TotalVertexCount = 0;
        TotalFaceCount = 0;
        Array.Clear(Meshes);
        Array.Clear(MeshMemory);
    }
    
    
    public bool GetMeshData(
        int meshIndex,
        out NativeView<Vertex3D> vertices,
        out NativeView<SkinningData> skinned,
        out NativeView<byte> indices)
    {
        if(MeshCount == 0) Throwers.InvalidOperation(nameof(MeshCount));
        
        var mesh = Meshes[meshIndex];
        var block = MeshMemory[meshIndex];
        var is16Bit = mesh.Info.VertexCount < ushort.MaxValue;

        vertices = block.Data.Reinterpret<Vertex3D>();
        block = block.Next;

        if (block.IsNull) Throwers.NullPointer("Index block is null");

        indices = block.Data;
        if (is16Bit) indices = indices.Slice(0, indices.Length / 2);

        block = block.Next;

        if (mesh.Info.BoneCount > 0)
        {
            if (block.IsNull) Throwers.NullPointer("Skinned block is null");
            skinned = block.Data.Reinterpret<SkinningData>();
        }
        else
        {
            skinned = default;
        }

        return is16Bit;
    }

}

internal sealed class AnimationImportContext
{
    public int BoneCount;
    public int AnimationCount;
    
    public readonly byte[] ParentIndices = new byte[BoneLimit];
    public readonly Matrix4x4[] BindPose = new Matrix4x4[BoneLimit];
    public readonly Matrix4x4[] InverseBindPose = new Matrix4x4[BoneLimit];
    
    public AnimationClip[] Clips = new AnimationClip[16];
    public NativeArray<byte> ClipBuffer = NativeArray<byte>.MakeNull();
    
    public ModelRig Compile(Dictionary<string, int> boneMapping)
    {
        int boneCount = BoneCount,animationCount = AnimationCount;
        if(boneCount != boneMapping.Count)
            Throwers.InvalidOperation("Bone count does not match bone mapping count");
        if(boneCount == 0 || animationCount == 0) 
            Throwers.InvalidOperation("Bone or animation count is zero");
        
        return new ModelRig(
            boneMapping: new Dictionary<string, int>(boneMapping),
            parentIndices: ParentIndices.AsSpan(0, boneCount),
            bindPose: BindPose.AsSpan(0, boneCount),
            inverseBindPose: InverseBindPose.AsSpan(0, boneCount),
            clips: Clips.AsSpan(0, animationCount),
            clipsBuffer:ClipBuffer
        );
    }

    
    public void Begin(AssimpSceneMeta meta)
    {
        if(AnimationCount > 0 || BoneCount > 0 || !ClipBuffer.IsNull)
            Throwers.InvalidOperation("Context already initialized");

        BoneCount = meta.BoneCount;
        AnimationCount = meta.AnimationCount;
        if (meta.AnimationCount > Clips.Length)
            Array.Resize(ref Clips, int.Max(meta.AnimationCount, Clips.Length * 2));
    }
    
    public void Reset()
    {
        BoneCount = 0;
        AnimationCount = 0;
        Array.Clear(ParentIndices);
        Array.Clear(BindPose);
        Array.Clear(InverseBindPose);
        Array.Clear(Clips);
        ClipBuffer = NativeArray<byte>.MakeNull();
    }
}
internal sealed class EmbeddedImportContext(TextureLoader textureLoader)
{
    public int TextureCount;
    public int MaterialCount;
    public readonly List<EmbeddedSceneTexture> Textures = new(16);
    public readonly List<EmbeddedSceneMaterial> Materials = new(16);

    public unsafe void RegisterTexture(EmbeddedSceneTexture texture, byte* data, int length)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        textureLoader.StoreEmbedded(texture.GId, data, length, texture.PixelFormat, out texture.Dimensions);
    }

    public void Compile(List<IEmbeddedAsset> embeddedSink)
    {
        if(TextureCount != Textures.Count || MaterialCount != Materials.Count)
            Throwers.InvalidOperation("Texture count or material count mismatch");
        
        if (TextureCount > 0)
        {
            Textures.Sort(static (it1, it2) => it1.TextureIndex.CompareTo(it2.TextureIndex));
            embeddedSink.AddRange(Textures);
        }

        if (MaterialCount > 0)
        {
            Materials.Sort(static (it1, it2) => it1.MaterialIndex.CompareTo(it2.MaterialIndex));
            embeddedSink.AddRange(Materials);
        }
    }

    public void Begin(AssimpSceneMeta meta)
    {
        if(TextureCount > 0 || MaterialCount > 0 || Textures.Count > 0 || Materials.Count > 0)
            Throwers.InvalidOperation("Context already initialized");

        TextureCount = meta.TextureCount;
        MaterialCount = meta.MaterialCount;
    }
    
    public void Reset()
    {
        TextureCount = 0;
        MaterialCount = 0;
        Textures.Clear();
        Materials.Clear();
    }
}
