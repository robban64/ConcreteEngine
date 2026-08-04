using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

[Inspect]
public sealed class Material : AssetObject
{
    public Id16<Material> MaterialId => State.MaterialId;
    public MaterialProfileId ProfileId { get; private set; }

    [InspectInclude] public readonly MaterialState State;

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


    public void ClearSourceSlot(int slot)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_textureSources.Length);
        _textureSources[slot] = default;
    }
    public void SetSourceSlot(int slot, Texture texture)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_textureSources.Length);
        ref var source = ref _textureSources[slot];
        source = source.WithTexture(texture.Id, texture.Profile);
        if (source.Usage == TextureUsage.Mask) State.HasAlphaMask = source.IsBound();
        MarkDirty(AssetDirtyFlag.State);
    }

    public void SetSourceSlot(int slot, TextureId textureId, SamplerProfile profile)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)slot, (uint)_textureSources.Length);
        ref var source = ref _textureSources[slot];
        source = source.WithTexture(default, profile, textureId);
        if (source.Usage == TextureUsage.Mask) State.HasAlphaMask = source.IsBound();
        MarkDirty(AssetDirtyFlag.State);
    }

}