using ConcreteEngine.Core.Common;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Shaders;

internal sealed class ShaderLoaderModule : AssetTypeLoader<Shader, ShaderRecord>
{
    private ShaderImporter _shaderImporter;
    private AssetGfxUploader _uploader;

    public ShaderLoaderModule(AssetGfxUploader uploader) : base(uploader)
    {
        _uploader = uploader;
        _shaderImporter = new ShaderImporter();
    }

    public override void Setup()
    {
        _shaderImporter.ImportAllDefinitions();
        IsActive = true;
    }

    public override void Teardown()
    {
        _shaderImporter.ClearCache();
        _shaderImporter = null!;
        _uploader = null!;
        IsActive = false;
    }


    protected override Shader Load(ShaderRecord record, LoaderContext ctx)
    {
        var (vsFile, fsFile) = ShaderRecord.GetFilenames(record);
        var vertPath = Path.Combine(EnginePath.ShaderCorePath, vsFile);
        var fragPath = Path.Combine(EnginePath.ShaderCorePath, fsFile);

        var vertInfo = new FileInfo(vertPath);
        if (!vertInfo.Exists) throw new FileNotFoundException("File not found.", vertPath);

        var fragInfo = new FileInfo(fragPath);
        if (!fragInfo.Exists) throw new FileNotFoundException("File not found.", fragPath);

        var vertResult = _shaderImporter.ImportShader(vertPath);
        var fragResult = _shaderImporter.ImportShader(fragPath);

        var payload = new ShaderPayload(vertResult, fragResult, vertInfo.Length, fragInfo.Length);
        _uploader.UploadShader(in payload, out var info);

        return new Shader
        {
            Id = ctx.Id,
            GId = ctx.GId,
            ResourceId = info.ShaderId,
            Name = record.Name,
            Samplers = info.Samplers,
            IsCoreAsset = true
        };
    }

    protected override Shader LoadEmbedded(EmbeddedRecord embedded, LoaderContext context) =>
        throw new NotImplementedException();


    public void ReloadShader(Shader shader, AssetFileSpec[] prevFileSpecs, out AssetFileSpec[] fileSpecs)
    {
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

        var vertResult = _shaderImporter.ImportShader(vertPath);
        var fragResult = _shaderImporter.ImportShader(fragPath);

        var payload = new ShaderPayload(vertResult, fragResult, vertInfo.Length, fragInfo.Length);
        _uploader.RecreateShader(shader.ResourceId, in payload, out var info);

        fileSpecs = new AssetFileSpec[2];
        fileSpecs[0] = prevFileSpecs[0] with { SizeBytes = payload.VsSize };
        fileSpecs[1] = prevFileSpecs[1] with { SizeBytes = payload.FsSize };

        shader.OnReload(info.Samplers);
    }
}