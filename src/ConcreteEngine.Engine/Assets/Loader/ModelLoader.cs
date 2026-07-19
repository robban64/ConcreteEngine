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
        DefaultLength * Unsafe.SizeOf<VertexShading>() +
        DefaultLength * Unsafe.SizeOf<SkinningData>() +
        DefaultLength * Unsafe.SizeOf<uint>() * 3 +
        DefaultLength; // clips

    //
    private ModelImporter? _importer;
    private NativeArray<byte> _importBuffer;

    public readonly List<IEmbeddedAsset> EmbeddedAssets = new(16);

    protected override void OnActivate()
    {
        _importBuffer = NativeArray.Allocate(TotalSize, false);

        _importer = new ModelImporter(textureLoader);
    }

    protected override void OnDeActivate()
    {
        EmbeddedAssets.Clear();

        _importBuffer.Dispose();

        _importer?.Dispose();
        _importer = null;
    }


    protected override Model Load(ModelRecord record, ImportContext ctx)
    {
        if (_importBuffer.IsNull) throw new InvalidOperationException("Allocator is null");
        if (_importer is not { } importer) throw new InvalidOperationException("ModelImport is null");
        if (EmbeddedAssets.Count > 0) throw new InvalidOperationException("EmbeddedAssets is not empty");

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

        var model = new Model(record.Name, ctx.Id, record.Id, in modelInfo, in bounds, meshes, rig);

        importer.Cleanup();
        return model;
    }


    private void AllocMeshBlocks(MeshImportContext context)
    {
        if (_importBuffer.IsNull) throw new InvalidOperationException("Allocator is null");
        var meshLength = context.MeshCount;
        if (meshLength == 0) throw new InvalidOperationException("Model import resulted in zero meshes");

        var cursor = 0;
        var allocator = new NativeAllocator(_importBuffer, ref cursor);
        for (int i = 0; i < meshLength; i++)
        {
            var info = context.Meshes[i].Info;

            var indexStride = info.Has16BitIndex ? sizeof(ushort) : sizeof(uint);
            context.MeshData[i] = new MeshImportData
            {
                Vertices = allocator.AllocSlice<VertexShading>(info.VertexCount),
                Indices = allocator.AllocSlice(info.TrisCount * indexStride * 3),
                Skinning = info.BoneCount > 0 ? allocator.AllocSlice<SkinningData>(info.VertexCount) : default
            };
        }
    }


    protected override Model LoadInMemory(ModelRecord record, ImportContext ctx) => throw new NotImplementedException();


    public MeshId UploadMesh(in MeshImportData data, bool is16Bit)
    {
        var drawSize = is16Bit ? DrawElementSize.UnsignedShort : DrawElementSize.UnsignedInt;
        var indices = data.Indices.Length / (is16Bit ? sizeof(ushort) : sizeof(uint));
        var properties = MeshDrawProperties.MakeElemental(drawCount: indices, size: drawSize);
        var iboArgs = CreateIboArgs.MakeDefault();

        var meshId = gfx.CreateEmptyMesh(in properties, 2, VertexAttributes.GetVertex3DAttributesV2());
        gfx.CreateAttachVertexBuffer(meshId, data.Positions.AsReadOnlySpan(), CreateVboArgs.MakeDefault(0));
        gfx.CreateAttachVertexBuffer(meshId, data.Vertices.AsReadOnlySpan(), CreateVboArgs.MakeDefault(1));
        if (is16Bit)
            gfx.CreateAttachIndexBuffer(meshId, data.Indices.Reinterpret<ushort>().AsReadOnlySpan(), iboArgs);
        else
            gfx.CreateAttachIndexBuffer(meshId, data.Indices.Reinterpret<uint>().AsReadOnlySpan(), iboArgs);
        return meshId;
    }

    public MeshId UploadAnimatedMesh(in MeshImportData data, bool is16Bit)
    {
        var drawSize = is16Bit ? DrawElementSize.UnsignedShort : DrawElementSize.UnsignedInt;
        var indices = data.Indices.Length / (is16Bit ? sizeof(ushort) : sizeof(uint));
        var properties = MeshDrawProperties.MakeElemental(drawCount: indices, size: drawSize);
        var iboArgs = CreateIboArgs.MakeDefault();

        var meshId = gfx.CreateEmptyMesh(in properties, 3, VertexAttributes.GetSkinnedAttributesV2());
        gfx.CreateAttachVertexBuffer(meshId, data.Positions.AsReadOnlySpan(), CreateVboArgs.MakeDefault(0));
        gfx.CreateAttachVertexBuffer(meshId, data.Vertices.AsReadOnlySpan(), CreateVboArgs.MakeDefault(1));
        gfx.CreateAttachVertexBuffer(meshId, data.Skinning.AsReadOnlySpan(), CreateVboArgs.MakeDefault(2));
        if (is16Bit)
            gfx.CreateAttachIndexBuffer(meshId, data.Indices.Reinterpret<ushort>().AsReadOnlySpan(), iboArgs);
        else
            gfx.CreateAttachIndexBuffer(meshId, data.Indices.Reinterpret<uint>().AsReadOnlySpan(), iboArgs);
        return meshId;
    }
}