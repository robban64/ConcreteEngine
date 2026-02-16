using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Controller.Proxy;

public sealed class AssetObjectProxy(IAsset asset, AssetFileSpec[] fileSpecs)
{
    public readonly IAsset Asset = asset;

    public readonly AssetFileSpec[] FileSpecs = fileSpecs;

    public required IAssetProxyProperty Property;

    //public string GIdString { get; } = asset.GId.ToString();
}

public interface IAssetProxyProperty
{
    Type AssetType { get; }
}

public abstract class AssetProxyProperty<T>(T asset) : IAssetProxyProperty where T : class, IAsset
{
    public readonly T Asset = asset;
    public Type AssetType => typeof(T);
}

public sealed class MaterialProxyProperty(IMaterial asset, in MaterialParams param, MaterialPipeline pipeline)
    : AssetProxyProperty<IMaterial>(asset)
{
    public MaterialParams Params = param;
    public MaterialPipeline Pipeline = pipeline;

    public required IMaterial? TemplateMaterial;
    public required IShader Shader;
    public required ITexture?[] Textures;
    public required TextureSource[] Bindings;

    public required Action<MaterialProxyProperty> CommitDel;
    public required Action<MaterialProxyProperty> FetchDel;
    public void Commit() => CommitDel(this);
    public void Fetch() => FetchDel(this);
}

public sealed class TextureProxyProperty(ITexture asset) : AssetProxyProperty<ITexture>(asset)
{
    public float LodLevel = asset.LodBias;
    public TexturePreset Preset = asset.Preset;
    public AnisotropyLevel Anisotropy = asset.Anisotropy;
    public TextureUsage Usage = asset.Usage;
    public TexturePixelFormat PixelFormat = asset.PixelFormat;
}

public sealed class ShaderProxyProperty(IShader asset) : AssetProxyProperty<IShader>(asset)
{
}

public sealed class ModelProxyProperty(IModel asset, InspectorEditorObject inspector)
    : AssetProxyProperty<IModel>(asset)
{
    public readonly InspectorEditorObject Inspector = inspector;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Draw(in FrameContext ctx)
    {
        var sw = ctx.Writer;
        var inspector = Inspector;
        
        inspector.Header.Draw(in StyleMap.GetAssetColor(Asset.Kind), sw);
        
        //
        foreach (var prop in inspector.Sections)
        {
            prop.Draw();
        }
        
        inspector.ArrayUi?.Draw(sw);
    }
    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Draw(in FrameContext ctx)
    {
        var sw = ctx.Writer;
        var items = Inspector.Items;
        foreach (var item in items)
        {
            ImGui.Spacing();
            ImGui.TextUnformatted(ref sw.Write(item.FieldName));
            if (item.Info.Length > 0)
            {
                ImGui.SameLine();
                ImGui.TextUnformatted(ref sw.Start('[').Append(item.Info).Append(']').End());
            }

            ImGui.Separator();
            item.Draw(in ctx);
        }
        Inspector.EndFrame();
    }*/
}
/*
public sealed class ModelProxyProperty(IModel asset) : AssetProxyProperty<IModel>(asset)
{
    public required MeshPart[] Meshes;
    public required Clip[] Clips;

    public required int BoneCount;

    public sealed class MeshPart(string name, MeshId gfxId, MeshInfo info)
    {
        public string Name = name;
        public MeshId GfxId = gfxId;
        public MeshInfo Info = info;
    }

    public sealed class Clip(string name, int trackCount, float duration, float ticksPerSecond)
    {
        public string Name = name;
        public int TrackCount = trackCount;
        public float Duration = duration;
        public float TicksPerSecond = ticksPerSecond;
    }
}*/