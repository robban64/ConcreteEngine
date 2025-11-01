#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Descriptors;
using ConcreteEngine.Core.Assets.Internal;
using ConcreteEngine.Core.Assets.IO;

#endregion

namespace ConcreteEngine.Core.Assets.Shaders;

internal sealed class ShaderLoaderModule(AssetGfxUploader uploader)
{
    private ShaderLoader _loader = new();

    public bool IsPrepared { get; private set; }

    public Shader LoadShader(AssetId assetId, ShaderDescriptor manifest, bool isCoreAsset, out AssetFileSpec[] specs)
    {
        if (!IsPrepared) Prepare();
        var basePath = isCoreAsset ? 
            Path.Combine(AssetPaths.CorePath, AssetPaths.ShaderFolder, "core-shaders")
            : Path.Combine(AssetPaths.AssetPath, AssetPaths.ShaderFolder);

        var payload = _loader.LoadShader(manifest, basePath);
        uploader.UploadShader(payload, out var info);
        specs = [payload.VertexFileSpec, payload.FragmentFileSpec];
        return new Shader
        {
            RawId = assetId,
            ResourceId = info.ShaderId,
            Name = manifest.Name,
            Samplers = info.Samplers,
            IsCoreAsset = isCoreAsset
        };
    }

    public void ReloadShader(Shader shader, AssetFileEntry[] files, out AssetFileSpec[] specs)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(files.Length, 2);

        if (!IsPrepared) Prepare();

        var vertIdx = files[0].RelativePath.Contains(".vert") ? 0 : 1;
        var vert = files[vertIdx];
        var frag = vertIdx == 0 ? files[1] : files[0];

        var basePath = shader.IsCoreAsset ? 
            Path.Combine(AssetPaths.CorePath, "core-shaders")
            : Path.Combine(AssetPaths.AssetPath, AssetPaths.ShaderFolder);
        var vPath = shader.IsCoreAsset
            ? AssetPaths.CoreShaderPath("core-shaders", vert.RelativePath)
            : AssetPaths.GetShaderPath(vert.RelativePath);
        var fPath = shader.IsCoreAsset
            ? AssetPaths.CoreShaderPath("core-shaders", frag.RelativePath)
            : AssetPaths.GetShaderPath(frag.RelativePath);

        
        
        var desc = new ShaderDescriptor(shader.Name, vPath, fPath);
        var payload = _loader.LoadShader(desc, basePath);
        uploader.RecreateShader(shader.ResourceId, payload, out var info);
        specs = [payload.VertexFileSpec, payload.FragmentFileSpec];

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