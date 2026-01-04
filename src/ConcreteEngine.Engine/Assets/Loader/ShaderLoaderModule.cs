using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Configuration.IO;
using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Engine.Assets.Shaders;

internal sealed class ShaderLoaderModule(AssetGfxUploader uploader)
{
    private ShaderLoader _loader = new();

    public bool IsPrepared { get; private set; }

    public Shader LoadShader(ShaderDescriptor manifest, ref LoadAssetContext ctx)
    {
        if (!IsPrepared) Prepare();
        var basePath = ctx.IsCore ? EnginePath.ShaderCorePath : EnginePath.ShaderPath;

        var payload = _loader.LoadShader(manifest, basePath);

        var vertArg = ctx.GetFileArgs();
        var fsArg = ctx.GetFileArgs();

        var vertFileSpec = new AssetFileSpec(
            Id: vertArg.Id,
            GId: vertArg.GId,
            Storage: AssetStorageKind.FileSystem,
            LogicalName: manifest.Name,
            RelativePath: manifest.VertexFilename,
            SizeBytes: payload.VsSize);

        var fragFileSpec = new AssetFileSpec(
            Id: fsArg.Id,
            GId: fsArg.GId,
            Storage: AssetStorageKind.FileSystem,
            LogicalName: manifest.Name,
            RelativePath: manifest.FragmentFilename,
            SizeBytes: payload.FsSize);

        ctx.FileSpecs = [vertFileSpec, fragFileSpec];

        uploader.UploadShader(payload, out var info);
        
        return new Shader
        {
            Id = ctx.Id,
            GId = ctx.GId,
            ResourceId = info.ShaderId,
            Name = manifest.Name,
            Samplers = info.Samplers,
            IsCoreAsset = ctx.IsCore
        };
    }

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

    public void Prepare()
    {
        if (IsPrepared) return;

        _loader.Prepare();
        IsPrepared = true;
    }

    public void Unload()
    {
        _loader.ClearCache();
        _loader = null!;
        IsPrepared = false;
    }
}