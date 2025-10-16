#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Assets.Shaders;
using ConcreteEngine.Core.Assets.Textures;
using ConcreteEngine.Core.Rendering.Definitions;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Materials;

public sealed class MaterialTemplate : AssetObject
{
    public override AssetKind Kind => AssetKind.Material;
    public override AssetCategory Category => AssetCategory.Renderer;
    
    public required AssetRef<Shader> ShaderRef { get; init; } 

    public required MaterialTemplateParams Params { get; init; }

    public MaterialTextureSlots TextureSlots { get; }
    

    internal MaterialTemplate(AssetTextureSlot[] samplerSlots)
    {
        TextureSlots = new MaterialTextureSlots(samplerSlots);
    }

}