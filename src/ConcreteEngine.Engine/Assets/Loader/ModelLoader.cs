using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.ImporterAssimp;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class ModelLoader(TextureLoader textureLoader, GfxMeshes gfx)
    : AssetTypeLoader<Model, ModelRecord>()
{
    private const int DefaultLength = 4096 * 32;

    private static readonly int TotalSize =
        DefaultLength * Unsafe.SizeOf<Vertex3D>() +
        DefaultLength * Unsafe.SizeOf<SkinningData>() +
        DefaultLength * Unsafe.SizeOf<uint>() * 3;

    //
    private ModelImporter? _importer;
    private ArenaAllocator? _allocator;

    public readonly List<IEmbeddedAsset> EmbeddedAssets = new(16);

    protected override void OnActivate()
    {
        _allocator = new ArenaAllocator(TotalSize, zeroed: false);
        _importer = new ModelImporter();
    }

    protected override void OnDeActivate()
    {
        EmbeddedAssets.Clear();
        
        _allocator?.Dispose();
        _allocator = null;

        _importer?.Dispose();
        _importer = null;
    }


    protected override Model Load(ModelRecord record, LoaderContext ctx)
    {
        if(_allocator is not {} allocator) throw new InvalidOperationException("Allocator is null");
        if (_importer is not {} importer) throw new InvalidOperationException("ModelImport is null");
        if (EmbeddedAssets.Count > 0) throw new InvalidOperationException("EmbeddedAssets is not empty");
        
        allocator.Clear();

        var filename = record.Files.First().Value;
        
        // load scene
        var modelContext = importer.StartImport(record.Name, EnginePath.ModelPath, filename);
        AllocMeshBlocks(modelContext);

        // write
        modelContext.SetTextureLoader(textureLoader);
        importer.ImportSceneData(modelContext);

        // upload
        importer.Upload(modelContext, this);

        // store
        var modelData = modelContext.Model;
        var animation = modelContext.Animation;

        var meshLength = (byte)modelData.Meshes.Length;
        if (meshLength == 0) throw new InvalidOperationException("Model import resulted in zero meshes");

        modelContext.SanitizeClips();

        ProcessEmbedded(modelContext);

        var modelInfo = new ModelInfo(
            vertexCount: modelData.TotalVertexCount,
            faceCount: modelData.TotalFaceCount,
            boneCount: (ushort)(animation?.BoneCount ?? 0),
            meshCount: meshLength,
            materialCount: (byte)modelContext.Materials.Count,
            textureCount: (byte)modelContext.Textures.Count,
            isAnimated: animation != null
        );

        importer.Cleanup();
        modelContext.Clear();

        return new Model(
            name: record.Name,
            id: ctx.Id,
            gid:record.GId,
            modelInfo: in modelInfo,
            bounds: in modelData.ModelBounds,
            meshes: modelData.Meshes,
            animation: animation
        );
    }


    private void AllocMeshBlocks(ModelImportContext modelContext)
    {
        if(_allocator is not {} allocator) throw new InvalidOperationException("Allocator is null");

        var modelImportData = modelContext.Model;
        for (int i = 0; i < modelImportData.Meshes.Length; i++)
        {
            var info = modelImportData.Meshes[i].Info;

            modelImportData.Blocks[i] = allocator.Alloc(info.VertexCount * Unsafe.SizeOf<Vertex3D>());
            allocator.Alloc(info.TrisCount * Unsafe.SizeOf<uint>() * 3);
            if (info.BoneCount > 0) allocator.Alloc(info.VertexCount * Unsafe.SizeOf<SkinningData>());
        }
    }

    private void ProcessEmbedded(ModelImportContext modelContext)
    {
        int textureLen = modelContext.Textures.Count, materialLen = modelContext.Materials.Count;

        if (textureLen > 0)
        {
            modelContext.Textures.Sort(static (it1, it2) => it1.TextureIndex.CompareTo(it2.TextureIndex));
            EmbeddedAssets.AddRange(modelContext.Textures);
        }

        if (materialLen > 0)
        {
            modelContext.Materials.Sort(static (it1, it2) => it1.MaterialIndex.CompareTo(it2.MaterialIndex));
            EmbeddedAssets.AddRange(modelContext.Materials);
        }
    }


    protected override Model LoadInMemory(ModelRecord record, LoaderContext ctx) => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public MeshId UploadMesh(NativeView<Vertex3D> vertices, NativeView<byte> indices, bool is16Bit)
    {
        var drawSize = is16Bit ? DrawElementSize.UnsignedShort : DrawElementSize.UnsignedInt;
        var properties = MeshDrawProperties.MakeElemental(drawCount: indices.Length, size: drawSize);
        var iboArgs = CreateIboArgs.MakeDefault();

        var meshId = gfx.CreateEmptyMesh(in properties, 1, VertexAttributes.GetVertex3DAttributes());
        gfx.CreateAttachVertexBuffer(meshId, vertices.AsReadOnlySpan(), CreateVboArgs.MakeDefault(0));
        if (is16Bit)
            gfx.CreateAttachIndexBuffer(meshId, indices.Reinterpret<ushort>().AsReadOnlySpan(), iboArgs);
        else
            gfx.CreateAttachIndexBuffer(meshId, indices.Reinterpret<uint>().AsReadOnlySpan(), iboArgs);
        return meshId;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public MeshId UploadAnimatedMesh(NativeView<Vertex3D> vertices, NativeView<SkinningData> skinned,
        NativeView<byte> indices, bool is16Bit)
    {
        var drawSize = is16Bit ? DrawElementSize.UnsignedShort : DrawElementSize.UnsignedInt;
        var properties = MeshDrawProperties.MakeElemental(drawCount: indices.Length, size: drawSize);
        var iboArgs = CreateIboArgs.MakeDefault();

        var meshId = gfx.CreateEmptyMesh(in properties, 2, VertexAttributes.GetSkinnedAttributes());
        gfx.CreateAttachVertexBuffer(meshId, vertices.AsReadOnlySpan(), CreateVboArgs.MakeDefault(0));
        gfx.CreateAttachVertexBuffer(meshId, skinned.AsReadOnlySpan(), CreateVboArgs.MakeDefault(1));
        if (is16Bit)
            gfx.CreateAttachIndexBuffer(meshId, indices.Reinterpret<ushort>().AsReadOnlySpan(), iboArgs);
        else
            gfx.CreateAttachIndexBuffer(meshId, indices.Reinterpret<uint>().AsReadOnlySpan(), iboArgs);
        return meshId;
    }
}