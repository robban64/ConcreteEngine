using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class Material : AssetObject
{
    public static Material FallbackMaterial { get; internal set; } = null!;

    public MaterialId MaterialId => State.MaterialId;

    public AssetId TemplateId { get; init; }
    public MaterialProfile Profile { get; private set; }

    public Shader BoundShader { get; private set; }

    public readonly MaterialState State;
    
    public override AssetCategory Category => AssetCategory.Renderer;
    public override AssetKind Kind => AssetKind.Material;

    private Material(string name, AssetId id, Guid gid, AssetId templateId, Shader? boundShader,
        MaterialProfile profile,
        TextureSource[] sources) : base(name, id, gid)
    {
        ArgumentNullException.ThrowIfNull(sources);

        TemplateId = templateId;
        Profile = profile;
        State = new MaterialState(this, sources);

        SetShader(boundShader);
        MarkDirty(AssetDirtyFlag.Lifecycle | AssetDirtyFlag.State | AssetDirtyFlag.Structure);
    }

    public Material(string name, AssetId id, Guid gid, AssetId templateId, Shader? boundShader, MaterialProfile profile,
        in MaterialParams param,
        TextureSource[] sources) : this(name, id, gid, templateId, boundShader, profile, sources)
    {
        State.SetParams(in param);
    }

    public Material(string name, AssetId id, Guid gid, AssetId templateId, Shader? boundShader, MaterialProfile profile,
        MaterialParamsRecord param, TextureSource[] sources) : this(name, id, gid, templateId, boundShader, profile,
        sources)
    {
        ArgumentNullException.ThrowIfNull(param);

        FromParamRecord(param);
    }


    public void SetShader(Shader? newShader)
    {
        if (newShader is not { } shader)
            shader = Shader.FallbackShader;
        
        if (BoundShader == shader) return;

        if (shader.HasShadowSampler) State.PassMasks |= PassMask.Depth;
        else State.PassMasks &= ~PassMask.Depth;

        BoundShader = shader;
        MarkDirty(AssetDirtyFlag.Dependencies);
    }


    internal Material MakeNewAsTemplate(AssetId newId, Guid newGId, string newName)
    {
        State.FillParams(out var param);
        return new Material(newName, newId, newGId, Id, BoundShader, Profile, in param, State.TextureSources);
    }

    private void FromParamRecord(MaterialParamsRecord param)
    {
        if (param.Color is { } color) State.Color = color;
        if (param.Shininess is { } shininess) State.Shininess = shininess;
        if (param.UvRepeat is { } uvRepeat) State.UvRepeat = uvRepeat;
        if (param.Specular is { } spec) State.Specular = spec;
    }
}
