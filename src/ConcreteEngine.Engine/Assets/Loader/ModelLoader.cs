using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
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
        DefaultLength * Unsafe.SizeOf<uint>() * 3 +
        DefaultLength; // clips

    //
    private ModelImporter? _importer;
    private ArenaAllocator? _allocator;

    public readonly List<IEmbeddedAsset> EmbeddedAssets = new(16);

    protected override void OnActivate()
    {
        _allocator = new ArenaAllocator(TotalSize, zeroed: false);
        _importer = new ModelImporter(textureLoader);
    }

    protected override void OnDeActivate()
    {
        EmbeddedAssets.Clear();

        _allocator?.Dispose();
        _allocator = null;

        _importer?.Dispose();
        _importer = null;
    }


    protected override Model Load(ModelRecord record, ImportContext ctx)
    {
        if (_allocator is not { } allocator) throw new InvalidOperationException("Allocator is null");
        if (_importer is not { } importer) throw new InvalidOperationException("ModelImport is null");
        if (EmbeddedAssets.Count > 0) throw new InvalidOperationException("EmbeddedAssets is not empty");

        allocator.Clear();

        var filePath = ctx.GetFile(1).RelativePath;

        // load scene
        var modelContext = importer.StartImport(record.Name, filePath);

        AllocMeshBlocks(modelContext.MeshContext);

        // write
        importer.ImportSceneData();

        // upload
        importer.Upload(this);

        // store
        var modelInfo = modelContext.Compile(EmbeddedAssets, out var meshes, out var rig);
        var bounds = modelContext.MeshContext.ModelBounds;


        var model = new Model(
            name: record.Name,
            id: ctx.Id,
            gid: record.Id,
            modelInfo: in modelInfo,
            bounds: in bounds,
            meshes: meshes,
            rig: rig
        );

        importer.Cleanup();
        return model;
    }


    private void AllocMeshBlocks(MeshImportContext context)
    {
        if (_allocator is not { } allocator) throw new InvalidOperationException("Allocator is null");
        var meshLength = context.MeshCount;
        if (meshLength == 0) throw new InvalidOperationException("Model import resulted in zero meshes");

        for (int i = 0; i < meshLength; i++)
        {
            var info = context.Meshes[i].Info;

            context.MeshMemory[i] = allocator.Alloc(info.VertexCount * Unsafe.SizeOf<Vertex3D>());
            allocator.Alloc(info.TrisCount * Unsafe.SizeOf<uint>() * 3);
            if (info.BoneCount > 0) allocator.Alloc(info.VertexCount * Unsafe.SizeOf<SkinningData>());
        }
    }


    protected override Model LoadInMemory(ModelRecord record, ImportContext ctx) => throw new NotImplementedException();

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