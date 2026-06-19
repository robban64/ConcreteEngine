using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Renderer.Buffer;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets;


public sealed class Material : AssetObject
{
    public Id16<MaterialSlot> MaterialId => State.MaterialId;
    public MaterialProfileId ProfileId { get; private set; }

    public readonly MaterialState State;
    private TextureSource[] _textureSources = [];

    public override AssetCategory Category => AssetCategory.Renderer;
    public override AssetKind Kind => AssetKind.Material;

    public Material(string name, AssetId id, Guid gid, MaterialProfileId profileId)
        : base(name, id, gid)
    {
        State = new MaterialState(this);

        SetProfile(profileId);
        MarkDirty(AssetDirtyFlag.Lifecycle | AssetDirtyFlag.State | AssetDirtyFlag.Structure);
    }

    public Material(string name, AssetId id, Guid gid, MaterialProfileId profileId, MaterialStateRecord? state)
        : this(name, id, gid, profileId)
    {
        state?.WriteTo(State);
    }

    public int SourceCount => _textureSources.Length;
    public ReadOnlySpan<TextureSource> GetSourceSpan() => _textureSources;

    public Shader BoundShader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AssetManager.GetMaterialProfile(ProfileId).Shader;
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



    public void SetSourceSlot(int slot, AssetId assetId, TextureId textureId = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_textureSources.Length);
        ref var source = ref _textureSources[slot];
        source = source.WithTexture(assetId, textureId);
        if (source.Usage == TextureUsage.Mask) State.HasAlphaMask = source.IsBound();
        MarkDirty(AssetDirtyFlag.State);
    }

    public void SetTextureSlot(int slot, Texture? texture) =>
        SetSourceSlot(slot, texture?.Id ?? default, texture?.GfxId ?? default);
    
    public void SetSources(ReadOnlySpan<TextureSource> sources)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(sources.Length, _textureSources.Length, nameof(sources));
        var profile = AssetManager.GetMaterialProfile(ProfileId);
        profile.ValidateSources(sources);
        for (var i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            SetSourceSlot(i, source.AssetTexture, source.OverrideTexture);
        }
    }


}