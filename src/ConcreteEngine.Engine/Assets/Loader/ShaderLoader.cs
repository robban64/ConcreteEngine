using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Loader.Importer;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class ShaderLoader(AssetGfxUploader uploader) : AssetTypeLoader<Shader, ShaderRecord>(uploader)
{
    public override int SetupAllocSize => ShaderImporter.ShaderBlockSize * 8;
    public override int DefaultAllocSize => ShaderImporter.ShaderBlockSize * 2;

    private ShaderImporter? _shaderImporter;
    private AssetGfxUploader _uploader = uploader;

    private readonly Dictionary<string, IntPtr> _blocks = new(16);

    private ArenaBlockPtr _vsBlock = null;
    private ArenaBlockPtr _fsBlock = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override void OnSetup()
    {
        _shaderImporter = new ShaderImporter();
        _shaderImporter.ImportAllDefinitions();

        if(!IsSetup)
        {
            _vsBlock = Allocator.AllocBlock(ShaderImporter.ShaderBlockSize);
            _fsBlock = Allocator.AllocBlock(ShaderImporter.ShaderBlockSize);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override void OnTeardown()
    {
        _shaderImporter?.ClearCache();
        _shaderImporter = null!;
        _uploader = null!;

        _vsBlock = null;
        _fsBlock = null;
        _blocks.Clear();
    }

    public void LoadAllShaders(Queue<AssetRecord> queue)
    {
        if (_shaderImporter == null) throw new InvalidOperationException("ShaderImporter is null");
        var arenaAllocator = Allocator;

        foreach (var record in queue)
        {
            var (vsFile, fsFile) = ShaderRecord.GetFilenames((ShaderRecord)record);
            if (!_blocks.ContainsKey(vsFile))
                ImportShaderFile(arenaAllocator, vsFile);

            if (!_blocks.ContainsKey(fsFile))
                ImportShaderFile(arenaAllocator, fsFile);
        }

        return;

        void ImportShaderFile(ArenaAllocator allocator, string filename)
        {
            var path = Path.Combine(EnginePath.ShaderCorePath, filename);

            var allocBuilder = allocator.AllocBuilder();
            var dataPtr = allocBuilder.Memory.DataPtr;
            var span = _shaderImporter.ImportShader(path, dataPtr, out _);
            allocBuilder.AllocSlice(span.Length);

            _blocks.Add(filename, (IntPtr)allocBuilder.Commit());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override Shader Load(ShaderRecord record, LoaderContext ctx)
    {
        if (_shaderImporter == null) throw new InvalidOperationException("ShaderImporter is null");

        var (vsFile, fsFile) = ShaderRecord.GetFilenames(record);

        var vsPtr = (ArenaBlockPtr)_blocks[vsFile];
        var fsPtr = (ArenaBlockPtr)_blocks[fsFile];
        if (vsPtr.IsNull || vsPtr.Length <= 0) throw new InvalidOperationException("Vertex Shader pointer is null");
        if (fsPtr.IsNull || fsPtr.Length <= 0) throw new InvalidOperationException("Fragment Shader pointer is null");

        _uploader.UploadShader(vsPtr.DataPtr, fsPtr.DataPtr, out var info);

        return new Shader(record.Name)
        {
            Id = ctx.Id,
            GId = record.GId,
            GfxId = info.ShaderId,
            Samplers = info.Samplers,
            IsCoreAsset = true
        };
    }

    protected override Shader LoadInMemory(ShaderRecord record, LoaderContext ctx)
        => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ReloadShader(Shader shader, AssetFileSpec[] prevFileSpecs, out AssetFileSpec[] fileSpecs)
    {
        if (_shaderImporter == null) throw new InvalidOperationException("ShaderImporter is null");
        if (_vsBlock.IsNull || _fsBlock.IsNull) throw new InvalidOperationException(nameof(_vsBlock));

        ArgumentOutOfRangeException.ThrowIfNotEqual(prevFileSpecs.Length, 2);
        InvalidOpThrower.ThrowIf(!IsActive, nameof(IsActive));

        AssetFileSpec vsFile = prevFileSpecs[0], fsFile = prevFileSpecs[1];

        var vsPath = Path.Combine(EnginePath.ShaderCorePath, Path.GetFileName(vsFile.RelativePath));
        var fsPath = Path.Combine(EnginePath.ShaderCorePath, Path.GetFileName(fsFile.RelativePath));

        _shaderImporter.ImportShader(vsPath, _vsBlock.DataPtr, out var vsLength);
        _shaderImporter.ImportShader(fsPath, _fsBlock.DataPtr, out var fsLength);

        _uploader.RecreateShader(shader.GfxId, _vsBlock.DataPtr, _fsBlock.DataPtr, out _);

        fileSpecs = new AssetFileSpec[2];
        fileSpecs[0] = vsFile with { LastWriteTime = File.GetLastWriteTime(vsPath), SizeBytes = vsLength };
        fileSpecs[1] = fsFile with { LastWriteTime = File.GetLastWriteTime(fsPath), SizeBytes = fsLength };
    }
}