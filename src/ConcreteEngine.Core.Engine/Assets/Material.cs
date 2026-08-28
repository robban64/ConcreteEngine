using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets.Descriptors;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Core.Engine.Assets;

[Inspect]
public sealed class Material : AssetObject
{
    public Id16<Material> MaterialId => State.MaterialId;
    
    [InspectInclude] public readonly MaterialState State;


    private MaterialProfileId _profileId;
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
    
    [InputCombo]
    public MaterialProfileId ProfileId
    {
        get => _profileId;
        set => SetProfile(value);
    }


    public int SourceCount => _textureSources.Length;
    public ReadOnlySpan<TextureSource> GetSourceSpan() => _textureSources;

    public Shader BoundShader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AssetManager.GetMaterialProfile(_profileId).Shader;
    }

    public void SetProfile(MaterialProfileId profileId)
    {
        if (profileId == _profileId) return;
        var profileEntry = AssetManager.GetMaterialProfile(profileId);

        if (profileEntry.SlotsCount != _textureSources.Length)
            _textureSources = profileEntry.MakeSourceArray();
        else
            profileEntry.WriteSources(_textureSources);

        _profileId = profileId;
        State.SetFromProfile(profileEntry);
        MarkDirty(AssetDirtyFlag.Structure);
    }


    public void SetSourceSlot(Texture texture, SamplerSlot slot)
    {
        ref var source = ref GetTextureSource(slot);
        source = source with { AssetId = texture.Id, TextureId = texture.GfxId, Profile = texture.Profile };
        if (source.Slot == SamplerSlot.AlphaMask) State.HasAlphaMask = true;
        MarkDirty(AssetDirtyFlag.State);
    }

    public void SetSourceSlot(TextureId textureId, SamplerSlot slot, SamplerProfile profile)
    {
        ref var source = ref GetTextureSource(slot);
        if (source.TextureId == textureId) return;
        source = source with { AssetId = default, TextureId = textureId, Profile = profile };
        if (source.Slot == SamplerSlot.AlphaMask) State.HasAlphaMask = true;
        MarkDirty(AssetDirtyFlag.State);
    }

    private ref TextureSource GetTextureSource(SamplerSlot slot)
    {
        foreach (ref var textureSource in _textureSources.AsSpan())
        {
            if (textureSource.Slot == slot) return ref textureSource;
        }

        throw new ArgumentException(nameof(slot));
    }

    public void ClearSourceSlot(SamplerSlot slot)
    {
        ref var source = ref GetTextureSource(slot);
        if (source.IsFallback()) return;
        source = default;
        MarkDirty(AssetDirtyFlag.State);
    }
}