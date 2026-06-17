using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;


public sealed class Material : AssetObject
{
    public MaterialId MaterialId => State.MaterialId;
    public MaterialProfileId ProfileId { get; private set; }

    public readonly MaterialState State;
    private TextureSource[] _textureSources = [];

    public override AssetCategory Category => AssetCategory.Renderer;
    public override AssetKind Kind => AssetKind.Material;

    private Material(string name, AssetId id, Guid gid, MaterialProfileId profileId)
        : base(name, id, gid)
    {
        State = new MaterialState(this);

        SetProfile(profileId);
        MarkDirty(AssetDirtyFlag.Lifecycle | AssetDirtyFlag.State | AssetDirtyFlag.Structure);
    }

    public Material(string name, AssetId id, Guid gid, MaterialProfileId profileId, in MaterialParams param)
        : this(name, id, gid, profileId)
    {
        State.Albedo = param.Color;
        State.Specular = param.Specular;
        State.Shininess = param.Shininess;
        State.Uv = param.UvRepeat;
    }

    public Material(string name, AssetId id, Guid gid, MaterialProfileId profileId, MaterialParamsRecord param)
        : this(name, id, gid, profileId)
    {
        ArgumentNullException.ThrowIfNull(param);
        param.WriteTo(State);
    }

    public int SourceCount => _textureSources.Length;
    public ReadOnlySpan<TextureSource> GetSourceSpan() => _textureSources;

    public Shader BoundShader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AssetManager.GetMaterialProfile(ProfileId).Shader;
    }

    protected override void OnCommit()
    {
        if((DirtyFlags & AssetDirtyFlag.Structure) == 0) return;
        if (State.Transparency && State.DrawQueue == DrawCommandQueue.Opaque)
            State.DrawQueue = DrawCommandQueue.Transparent;
    }

    public void SetProfile(MaterialProfileId profileId)
    {
        if (profileId == ProfileId) return;
        var profileEntry = AssetManager.GetMaterialProfile(profileId);

        if (profileEntry.SlotsCount != _textureSources.Length)
            _textureSources = profileEntry.MakeSourceArray();
        else
            profileEntry.WriteSources(_textureSources);

        ProfileId = profileId;
        State.SetFromProfile(profileEntry);
        MarkDirty(AssetDirtyFlag.Structure);
    }


    public void SetSources(ReadOnlySpan<TextureSource> sources)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(sources.Length, _textureSources.Length, nameof(sources));
        var profile = AssetManager.GetMaterialProfile(ProfileId);
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

}