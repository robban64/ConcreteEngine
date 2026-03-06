using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Engine.Assets.Loader.Importer;
using ConcreteEngine.Engine.Configuration.IO;

namespace ConcreteEngine.Engine.Assets.Loader;

internal sealed class ShaderLoader(AssetGfxUploader uploader) : AssetTypeLoader<Shader, ShaderRecord>(uploader)
{
    private ShaderImporter? _shaderImporter;
    private AssetGfxUploader _uploader = uploader;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Setup()
    {
        _shaderImporter = new ShaderImporter();
        _shaderImporter.ImportAllDefinitions();
        IsActive = true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override void Teardown()
    {
        _shaderImporter?.ClearCache();
        _shaderImporter?.Dispose();
        _shaderImporter = null!;
        _uploader = null!;
        IsActive = false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    protected override Shader Load(ShaderRecord record, LoaderContext ctx)
    {
        if (_shaderImporter == null) throw new InvalidOperationException("ShaderImporter is null");

        var (vsFile, fsFile) = ShaderRecord.GetFilenames(record);

        ArgumentException.ThrowIfNullOrEmpty(vsFile);
        ArgumentException.ThrowIfNullOrEmpty(fsFile);

        var vertPath = Path.Combine(EnginePath.ShaderCorePath, vsFile);
        var fragPath = Path.Combine(EnginePath.ShaderCorePath, fsFile);

        var vertInfo = new FileInfo(vertPath);
        if (!vertInfo.Exists) throw new FileNotFoundException("File not found.", vertPath);

        var fragInfo = new FileInfo(fragPath);
        if (!fragInfo.Exists) throw new FileNotFoundException("File not found.", fragPath);

         _shaderImporter.ImportShader(vertPath, fragPath, out var vertResult, out var fragResult);

        var payload = new ShaderPayload(vertResult, fragResult, vertInfo.Length, fragInfo.Length);
        _uploader.UploadShader(in payload, out var info);

        return new Shader(record.Name)
        {
            Id = ctx.Id,
            GId = record.GId,
            GfxId = info.ShaderId,
            Samplers = info.Samplers,
            IsCoreAsset = true
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ReloadShader(Shader shader, AssetFileSpec[] prevFileSpecs, out AssetFileSpec[] fileSpecs)
    {
        if (_shaderImporter == null) throw new InvalidOperationException("ShaderImporter is null");

        ArgumentOutOfRangeException.ThrowIfNotEqual(prevFileSpecs.Length, 2);
        InvalidOpThrower.ThrowIf(!IsActive, nameof(IsActive));

        var vsFile = prevFileSpecs[0];
        var fsFile = prevFileSpecs[1];

        var vertPath = Path.Combine(EnginePath.ShaderCorePath, vsFile.RelativePath);
        var fragPath = Path.Combine(EnginePath.ShaderCorePath, fsFile.RelativePath);

        var vertInfo = new FileInfo(vertPath);
        if (!vertInfo.Exists) throw new FileNotFoundException("File not found.", vertPath);

        var fragInfo = new FileInfo(fragPath);
        if (!fragInfo.Exists) throw new FileNotFoundException("File not found.", fragPath);

        _shaderImporter.ImportShader(vertPath, fragPath, out var vertResult, out var fragResult);
        var payload = new ShaderPayload(vertResult, fragResult, vertInfo.Length, fragInfo.Length);
        _uploader.RecreateShader(shader.GfxId, in payload, out var info);

        fileSpecs = new AssetFileSpec[2];
        fileSpecs[0] = prevFileSpecs[0] with { SizeBytes = payload.VsSize };
        fileSpecs[1] = prevFileSpecs[1] with { SizeBytes = payload.FsSize };
    }
}