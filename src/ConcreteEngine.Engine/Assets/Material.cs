using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Descriptors;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Assets;

public sealed record Material : AssetObject, IMaterial
{
    internal static class DirtyState
    {
        public static readonly HashSet<MaterialId> DirtyIds = new(16);
    }

    public MaterialId MaterialId { get; internal set; }
    public AssetId TemplateId { get; init; }
    public AssetId AssetShader { get; init; }

    private MaterialParams _param;
    private MaterialProperties _properties;

    private readonly AssetTextureSlot[] _textureSlots;

    private bool _clearDirty;

    public override AssetCategory Category => AssetCategory.Renderer;
    public override AssetKind Kind => AssetKind.Material;

    public ReadOnlySpan<AssetTextureSlot> GetTextureSlots() => _textureSlots;

    internal Material(AssetId templateId, AssetId assetShader, in MaterialParams param,
        AssetTextureSlot[] slots)
    {
        ArgumentNullException.ThrowIfNull(slots);

        TemplateId = templateId;
        AssetShader = assetShader;
        _textureSlots = slots;
        _param = param;
        CalculateProperties();
    }

    internal Material(AssetId templateId, AssetId assetShader, MaterialParamsRecord param,
        AssetTextureSlot[] slots)
    {
        ArgumentNullException.ThrowIfNull(slots);

        TemplateId = templateId;
        AssetShader = assetShader;
        _textureSlots = slots;

        if (param.Color is { } color) _param.Color = color;
        if (param.Shininess is { } shininess) _param.Shininess = shininess;
        if (param.UvRepeat is { } uvRepeat) _param.UvRepeat = uvRepeat;
        if (param.Specular is { } spec) _param.Specular = spec;

        CalculateProperties();
    }

    internal bool IsDirty
    {
        get => DirtyState.DirtyIds.Contains(MaterialId);
        private set
        {
            if (Id == 0) return;
            if (value) DirtyState.DirtyIds.Add(MaterialId);
            else DirtyState.DirtyIds.Remove(MaterialId);
        }
    }

    public MaterialPipelineState Pipeline
    {
        get;
        set
        {
            field = value;
            IsDirty = true;
        }
    }

    public Color4 Color
    {
        get => _param.Color;
        set
        {
            _param.Color = value;
            IsDirty = true;
        }
    }

    public float Shininess
    {
        get => _param.Shininess;
        set
        {
            _param.Shininess = value;
            IsDirty = true;
        }
    }

    public float Specular
    {
        get => _param.Specular;
        set
        {
            _param.Specular = value;
            IsDirty = true;
        }
    }

    public float UvRepeat
    {
        get => _param.UvRepeat;
        set
        {
            _param.UvRepeat = value;
            IsDirty = true;
        }
    }

    public bool Transparency
    {
        get => _properties.HasTransparency;
        set
        {
            _properties.HasTransparency = value;
            IsDirty = true;
        }
    }

    public bool HasAlphaMask
    {
        get => _properties.HasAlphaMask;
        set
        {
            _properties.HasAlphaMask = value;
            IsDirty = true;
        }
    }

    public bool HasNormal
    {
        get => _properties.HasNormal;
        set
        {
            _properties.HasNormal = value;
            IsDirty = true;
        }
    }
    
    public bool HasShadowMap
    {
        get => _properties.HasShadowMap;
        set
        {
            _properties.HasShadowMap = value;
            IsDirty = true;
        }
    }

    internal void ClearDirty()
    {
        if (_clearDirty && IsDirty)
        {
            IsDirty = false;
            _clearDirty = false;
            return;
        }

        _clearDirty = true;
    }


    private void CalculateProperties()
    {
        ref var props = ref _properties;
        props = new MaterialProperties { HasTransparency = props.HasTransparency };

        foreach (var slot in _textureSlots)
        {
            if (!HasShadowMap) HasShadowMap = slot.SlotKind == MaterialSlotKind.Shadowmap;
            if (!slot.Asset.IsValid()) continue;
            if (!HasNormal) HasNormal = slot.SlotKind == MaterialSlotKind.Normal;
            if (!HasAlphaMask) HasAlphaMask = slot.SlotKind == MaterialSlotKind.Mask;
        }
    }

    public void FillPayload(ShaderId shaderId, out RenderMaterialPayload payload)
    {
        payload = new RenderMaterialPayload(MaterialId, shaderId, in _param, _properties, Pipeline);
    }
}