#region

using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Internal;

#endregion

namespace ConcreteEngine.Core.Assets.Shaders;

internal sealed class ShaderLoaderModule(AssetGfxUploader uploader)
{
    private ShaderLoader _loader = new();

    public Shader LoadShader(AssetId assetId, ShaderDescriptor manifest, out AssetFileSpec[] specs)
    {
        var payload = _loader.LoadShader(manifest);
        uploader.UploadShader(payload, out var info);
        specs = [payload.VertexFileSpec, payload.FragmentFileSpec];
        return new Shader
        {
            RawId = assetId,
            ResourceId = info.ShaderId,
            Name = manifest.Name,
            Samplers = info.Samplers,
            IsCoreAsset = false
        };
    }
    
    public void ReloadShader(Shader shader, AssetFileEntry vert, AssetFileEntry frag, out AssetFileSpec[] specs)
    {
        var desc = new ShaderDescriptor(shader.Name, vert.RelativePath, frag.RelativePath);
        var payload = _loader.LoadShader(desc);
        uploader.RecreateShader(shader.ResourceId, payload, out var info);
        specs = [payload.VertexFileSpec, payload.FragmentFileSpec];
        shader.OnReload(info.Samplers);
  
    }

    public void Prepare() => _loader.Prepare();

    public void Unload()
    {
        _loader.ClearCache();
        _loader = null!;
    }
}