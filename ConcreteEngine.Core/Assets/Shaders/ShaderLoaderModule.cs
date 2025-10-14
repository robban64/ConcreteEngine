#region

using ConcreteEngine.Core.Assets.Data;

#endregion

namespace ConcreteEngine.Core.Assets.Shaders;

internal sealed class ShaderLoaderModule(AssetGfxUploader uploader)
{
    private ShaderLoader _loader = new();

    public Shader LoadShader(AssetId assetId, ShaderDescriptor manifest,
        out AssetFileSpec[] fileSpecs)
    {
        var payload = _loader.LoadShader(manifest);
        uploader.UploadShader(payload, out var info);
        fileSpecs = [payload.VertexFileSpec, payload.FragmentFileSpec];
        return new Shader
        {
            RawId = assetId,
            ResourceId = info.ShaderId,
            Name = manifest.Name,
            Samplers = info.Samplers,
            IsCoreAsset = false,
            Generation = 0
        };
    }

    public void Prepare() => _loader.Prepare();

    public void Unload()
    {
        _loader.ClearCache();
        _loader = null!;
    }
}