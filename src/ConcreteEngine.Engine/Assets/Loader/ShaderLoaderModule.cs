using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Shaders;

internal sealed class ShaderLoaderModule : AssetTypeLoader<Shader, ShaderRecord>
{
    private readonly ShaderImporter _shaderImporter;
    private readonly AssetGfxUploader _uploader;

    public ShaderLoaderModule(AssetGfxUploader uploader) : base(uploader)
    {
        _uploader = uploader;
        _shaderImporter = new ShaderImporter();
        _shaderImporter.ImportAllDefinitions();
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

    public override void Teardown() => _shaderImporter.ClearCache();

    protected override Shader LoadEmbedded(EmbeddedRecord embedded, LoaderContext context) =>
        throw new NotImplementedException();
    
/*
    public void ReloadShader(Shader shader, AssetFileSpec[] prevFileSpecs, out AssetFileSpec[] fileSpecs)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(prevFileSpecs.Length, 2);

        if (!IsPrepared) Prepare();

        var vertIdx = prevFileSpecs[0].RelativePath.Contains(".vert") ? 0 : 1;
        var vert = prevFileSpecs[vertIdx];
        var frag = vertIdx == 0 ? prevFileSpecs[1] : prevFileSpecs[0];

        var basePath = shader.IsCoreAsset ? EnginePath.ShaderCorePath : EnginePath.ShaderPath;

        var desc = new ShaderDescriptor
        {
            Name = shader.Name, VertexFilename = vert.RelativePath, FragmentFilename = frag.RelativePath
        };
        var payload = _loader.LoadShader(desc, basePath);
        uploader.RecreateShader(shader.ResourceId, in payload, out var info);

        fileSpecs = new AssetFileSpec[2];
        fileSpecs[0] = prevFileSpecs[0] with{SizeBytes = payload.VsSize};
        fileSpecs[1] = prevFileSpecs[1] with{SizeBytes = payload.FsSize};


        shader.OnReload(info.Samplers);
    }
*/
}