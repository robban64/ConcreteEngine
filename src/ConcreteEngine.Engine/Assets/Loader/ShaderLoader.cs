using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Engine.Assets.Importer;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Assets.Loader;

public sealed class ShaderData
{
    public string Name;
    public MemoryBlock Memory;

    public ShaderData(string name, MemoryBlock memory)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (memory.IsNull) throw new ArgumentNullException(nameof(memory));
        Name = name;
        Memory = memory;
    }
}

internal sealed class ShaderLoader(GfxShaders gfxShaders) : AssetTypeLoader<Shader, ShaderRecord>
{
    private static int AllocSize => ShaderImporter.ShaderBlockSize * 8;

    private ShaderImporter? _shaderImporter;
    private BumpAllocator? _allocator;

    private readonly Dictionary<string, ShaderData> _data = new(16);

    private MemoryBlock _vsBlock = null;
    private MemoryBlock _fsBlock = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override void OnActivate()
    {
        _allocator = new BumpAllocator(AllocSize, blockSize: ShaderImporter.ShaderBlockSize, zeroed: false);

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
        _data.Clear();

        _allocator?.Dispose();
        _allocator = null;
    }

    public void ImportAllShaders(Queue<AssetRecord> queue)
    {
        if (_allocator is not { } allocator) throw new InvalidOperationException("Allocator is null");
        if (_shaderImporter is not { } importer) throw new InvalidOperationException("ShaderImporter is null");

        foreach (var record in queue)
        {
            var shaderRecord = (ShaderRecord)record;
            string vsFile = shaderRecord.VertexShader, fsFile = shaderRecord.FragmentShader;
            if (!_data.ContainsKey(vsFile))
            {
                var block = ImportShaderFile(allocator, importer, vsFile);
                _data.Add(vsFile, new ShaderData(vsFile, block));
            }

            if (!_data.ContainsKey(fsFile))
            {
                var block = ImportShaderFile(allocator, importer, fsFile);
                _data.Add(fsFile, new ShaderData(vsFile, block));
            }
        }

        return;

        static MemoryBlock ImportShaderFile(BumpAllocator allocator, ShaderImporter importer, string filename)
        {
            var path = Path.Join(EnginePath.ShaderCorePath, filename);

            var memory = allocator.AllocCommitBlock();
            var span = importer.ImportShader(path, memory.Data, out _);
            memory.AllocSlice(span.Length);
            return allocator.CommitBlock();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override Shader Load(ShaderRecord record, ImportContext ctx)
    {
        if (_allocator == null) throw new InvalidOperationException("Allocator is null");
        if (_shaderImporter == null) throw new InvalidOperationException("ShaderImporter is null");

        var vsPtr = _data[record.VertexShader].Memory;
        var fsPtr = _data[record.FragmentShader].Memory;

        if (vsPtr.IsNull || vsPtr.Length <= 0) throw new InvalidOperationException("Vertex Shader pointer is null");
        if (fsPtr.IsNull || fsPtr.Length <= 0) throw new InvalidOperationException("Fragment Shader pointer is null");

        var shaderId = gfxShaders.CreateShader(vsPtr.Data, fsPtr.Data, out var samplers);

        return new Shader(record.Name, ctx.Id, record.Id, shaderId, samplers);
    }

    protected override Shader LoadInMemory(ShaderRecord record, ImportContext ctx) =>
        throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Reload(Shader asset, AssetFile[] files)
    {
        if (!IsActive) Throwers.InvalidOperation(nameof(IsActive));
        if (_allocator == null) Throwers.InvalidOperation("Allocator is null");
        if (_shaderImporter == null) Throwers.InvalidOperation("ShaderImporter is null");
        if (_vsBlock.IsNull || _fsBlock.IsNull) Throwers.InvalidOperation(nameof(_vsBlock));

        ArgumentOutOfRangeException.ThrowIfNotEqual(files.Length, 3);

        AssetFile vsFile = files[1], fsFile = files[2];

        var vsPath = Path.Join(EnginePath.ShaderCorePath, Path.GetFileName(vsFile.RelativePath));
        var fsPath = Path.Join(EnginePath.ShaderCorePath, Path.GetFileName(fsFile.RelativePath));

        _shaderImporter.ImportShader(vsPath, _vsBlock.Data, out var vsLength);
        _shaderImporter.ImportShader(fsPath, _fsBlock.Data, out var fsLength);

        gfxShaders.RecreateShader(asset.GfxId, _vsBlock.Data, _fsBlock.Data, out var samplers);
        asset.SetSamplers(samplers);

        vsFile.LastWriteTime = File.GetLastWriteTime(vsPath);
        vsFile.SizeBytes = vsLength;

        fsFile.LastWriteTime = File.GetLastWriteTime(fsPath);
        fsFile.SizeBytes = fsLength;
    }
}