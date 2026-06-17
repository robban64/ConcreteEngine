using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;


public sealed class Material : AssetObject
{
    public MaterialId MaterialId => State.MaterialId;
    public MaterialProfile Profile { get; private set; }

    public readonly MaterialState State;
    private TextureSource[] _textureSources = [];

    public override AssetCategory Category => AssetCategory.Renderer;
    public override AssetKind Kind => AssetKind.Material;

    private Material(string name, AssetId id, Guid gid, MaterialProfile profile)
        : base(name, id, gid)
    {
        State = new MaterialState(this);

        SetProfile(profile);
        MarkDirty(AssetDirtyFlag.Lifecycle | AssetDirtyFlag.State | AssetDirtyFlag.Structure);
    }

    public Material(string name, AssetId id, Guid gid, MaterialProfile profile, in MaterialParams param)
        : this(name, id, gid, profile)
    {
        State.SetValues(in param);
    }

    public Material(string name, AssetId id, Guid gid, MaterialProfile profile, MaterialParamsRecord param)
        : this(name, id, gid, profile)
    {
        ArgumentNullException.ThrowIfNull(param);
        FromParamRecord(param);
    }

    public int SourceCount => _textureSources.Length;
    public ReadOnlySpan<TextureSource> GetSourceSpan() => _textureSources;

    public Shader BoundShader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AssetManager.GetMaterialProfile(Profile).Shader;
    }

    protected override void OnCommit()
    {
        if((DirtyFlags & AssetDirtyFlag.Structure) == 0) return;
        if (State.Transparency && State.DrawQueue == DrawCommandQueue.Opaque)
            State.DrawQueue = DrawCommandQueue.Transparent;
    }

    public void SetProfile(MaterialProfile profile)
    {
        if (profile == Profile) return;
        var profileEntry = AssetManager.GetMaterialProfile(profile);

        if (profileEntry.SlotsCount != _textureSources.Length)
            _textureSources = profileEntry.MakeSourceArray();
        else
            profileEntry.WriteSources(_textureSources);

        Profile = profile;
        State.SetFromProfile(profileEntry);
        MarkDirty(AssetDirtyFlag.Structure);
    }


    public void SetSources(ReadOnlySpan<TextureSource> sources)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(sources.Length, _textureSources.Length, nameof(sources));
        var profile = AssetManager.GetMaterialProfile(Profile);
        profile.ValidateSources(sources);
        sources.CopyTo(_textureSources);
    }

    public void SetSourceSlot(int slot, AssetId assetId, TextureId textureId = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_textureSources.Length);
        ref var source = ref _textureSources[slot];
        source = source with { AssetTexture = assetId, OverrideTextureId = textureId };
        MarkDirty(AssetDirtyFlag.State);
    }

    public void SetTextureSlot(int slot, Texture? texture) =>
        SetSourceSlot(slot, texture?.Id ?? default, texture?.GfxId ?? default);


    private void FromParamRecord(MaterialParamsRecord param)
    {
        if (param.Color is { } color) State.Color = color;
        if (param.Shininess is { } shininess) State.Shininess = shininess;
        if (param.UvRepeat is { } uvRepeat) State.UvRepeat = uvRepeat;
        if (param.Specular is { } spec) State.Specular = spec;
    }
}