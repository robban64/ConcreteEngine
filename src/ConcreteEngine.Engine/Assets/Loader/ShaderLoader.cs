using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.Importer;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class ShaderLoader(GfxShaders gfxShaders) : AssetTypeLoader<Shader, ShaderRecord>
{
    private static int AllocSize => ShaderImporter.ShaderBlockSize * 8;

    private ShaderImporter? _shaderImporter;
    private ArenaAllocator? _allocator;

    private readonly Dictionary<string, IntPtr> _blocks = new(16);

    private MemoryBlockPtr _vsBlock = null;
    private MemoryBlockPtr _fsBlock = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override void OnActivate()
    {
        _allocator = new ArenaAllocator(AllocSize, zeroed: false);

        _shaderImporter = new ShaderImporter();
        _shaderImporter.ImportAllDefinitions();

        if (!IsSetup)
        {
            _vsBlock = _allocator.AllocBlock(ShaderImporter.ShaderBlockSize);
            _fsBlock = _allocator.AllocBlock(ShaderImporter.ShaderBlockSize);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override void OnDeActivate()
    {
        _shaderImporter?.ClearCache();
        _shaderImporter = null!;

        _vsBlock = null;
        _fsBlock = null;
        _blocks.Clear();
        
        _allocator?.Dispose();
        _allocator = null;
    }

    public void LoadAllShaders(Queue<AssetRecord> queue)
    {
        if(_allocator is not {} allocator) throw new InvalidOperationException("Allocator is null");
        if (_shaderImporter is not {} importer) throw new InvalidOperationException("ShaderImporter is null");

        foreach (var record in queue)
        {
            var (vsFile, fsFile) = ShaderRecord.GetFilenames((ShaderRecord)record);
            if (!_blocks.ContainsKey(vsFile))
            {
                var block = ImportShaderFile(allocator, importer, vsFile);
                _blocks.Add(vsFile, (IntPtr)block);
            }
            if (!_blocks.ContainsKey(fsFile))
            {
                var block = ImportShaderFile(allocator, importer, fsFile);
                _blocks.Add(fsFile, (IntPtr)block);
            }
        }

        return;

        static MemoryBlockPtr ImportShaderFile(ArenaAllocator arena, ShaderImporter importer, string filename)
        {
            var path = Path.Join(EnginePath.ShaderCorePath, filename);

            var allocBuilder = arena.MakeBuilder();
            var span = importer.ImportShader(path, allocBuilder.Data, out _);
            allocBuilder.AllocSlice(span.Length);
            return arena.CommitBuilder(allocBuilder);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override Shader Load(ShaderRecord record, LoaderContext ctx)
    {
        if (_allocator == null) throw new InvalidOperationException("Allocator is null");
        if (_shaderImporter == null) throw new InvalidOperationException("ShaderImporter is null");

        var (vsFile, fsFile) = ShaderRecord.GetFilenames(record);

        var vsPtr = (MemoryBlockPtr)_blocks[vsFile];
        var fsPtr = (MemoryBlockPtr)_blocks[fsFile];
        
        if (vsPtr.IsNull || vsPtr.Length <= 0) throw new InvalidOperationException("Vertex Shader pointer is null");
        if (fsPtr.IsNull || fsPtr.Length <= 0) throw new InvalidOperationException("Fragment Shader pointer is null");

        var shaderId = gfxShaders.CreateShader(vsPtr.Data, fsPtr.Data, out var samplers);

        return new Shader(record.Name, ctx.Id, record.Id, shaderId, samplers);
    }

    protected override Shader LoadInMemory(ShaderRecord record, LoaderContext ctx) =>
        throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Reload(Shader asset, AssetFile[] files)
    {
        if (_allocator == null) throw new InvalidOperationException("Allocator is null");
        if (_shaderImporter == null) throw new InvalidOperationException("ShaderImporter is null");
        if (_vsBlock.IsNull || _fsBlock.IsNull) throw new InvalidOperationException(nameof(_vsBlock));

        ArgumentOutOfRangeException.ThrowIfNotEqual(files.Length, 3);
        InvalidOpThrower.ThrowIf(!IsActive, nameof(IsActive));

        AssetFile vsFile = files[1], fsFile = files[2];

        var vsPath = Path.Join(EnginePath.ShaderCorePath, Path.GetFileName(vsFile.RelativePath));
        var fsPath = Path.Join(EnginePath.ShaderCorePath, Path.GetFileName(fsFile.RelativePath));

        _shaderImporter.ImportShader(vsPath, _vsBlock.Data, out var vsLength);
        _shaderImporter.ImportShader(fsPath, _fsBlock.Data, out var fsLength);

        gfxShaders.RecreateShader(asset.GfxId, _vsBlock.Data, _fsBlock.Data, out var samplers);
        asset.SetSamplers(samplers);

        files[1] = vsFile with { LastWriteTime = File.GetLastWriteTime(vsPath), SizeBytes = vsLength };
        files[2] = fsFile with { LastWriteTime = File.GetLastWriteTime(fsPath), SizeBytes = fsLength };
    }
}