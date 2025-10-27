#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Internal;

#endregion

namespace ConcreteEngine.Core.Assets.Shaders;

internal sealed class ShaderLoaderModule(AssetGfxUploader uploader)
{
    private ShaderLoader _loader = new();
    
    public bool IsPrepared { get; private set; }

    public Shader LoadShader(AssetId assetId, ShaderDescriptor manifest, out AssetFileSpec[] specs)
    {
        if(!IsPrepared) Prepare();
        
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

    public void ReloadShader(Shader shader, AssetFileEntry[] files, out AssetFileSpec[] specs)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(files.Length, 2);

        if(!IsPrepared) Prepare();

        var vertIdx = files[0].RelativePath.Contains(".vert") ? 0 : 1;
        var vert = files[vertIdx];
        var frag = vertIdx == 0 ? files[1] : files[0];
        
        var desc = new ShaderDescriptor(shader.Name, vert.RelativePath, frag.RelativePath);
        var payload = _loader.LoadShader(desc);
        uploader.RecreateShader(shader.ResourceId, payload, out var info);
        specs = [payload.VertexFileSpec, payload.FragmentFileSpec];
        
        shader.OnReload(info.Samplers);
    }

    public void Prepare()
    {
        if(IsPrepared) return;
        
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