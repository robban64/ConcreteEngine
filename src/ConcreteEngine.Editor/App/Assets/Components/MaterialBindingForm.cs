using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib.Inputs;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets.Components;
/*
internal sealed class TextureInputField : InputField
{
    const float ImageThumbnailSize = 64f;
    private static float ImageOffset => ImageThumbnailSize + GuiTheme.FramePadding.X;

    private AssetId _assetId;
    private TextureId _gfxId;
    private SamplerProfile _profile;
    private TextureKind _kind;
    
    private TexturePtrHandle _textureHandle;

    private NativeString _name;

    public TextureInputField(string name) : base(name, InputKind.Texture)
    {
        _name = StringArena.AllocateString(32);
    }

    public void SetValue(ReadOnlySpan<AssetId> textureIds)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(textureIds.Length, _textureIds.Length);
        
        textureIds.CopyTo(_textureIds);
        _count = textureIds.Length;
    }

    public override bool Draw()
    {
        ImGui.BeginGroup();
        if (_gfxId.IsValid())
        {
            ImGui.AlignTextToFramePadding();
            AppDraw.Text(_name);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(_kind.ToUtf8());
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            AppDraw.TextColored(Palette32.OrangeBase, "Fallback"u8);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(_profile.ToUtf8());
        ImGui.EndGroup();

        var availWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SameLine(availWidth - ImageOffset);

        AppDraw.ImageButton("##img"u8, _gfxId, ref _textureHandle, new Vector2(ImageThumbnailSize));

        return false;
    }


    private void DrawSlot(int slot, string name, TextureId id, TextureKind kind, SamplerProfile profile)
    {
        ImGui.BeginGroup();
        if (id.IsValid())
        {
            ImGui.AlignTextToFramePadding();
            AppDraw.Text(name);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(kind.ToUtf8());
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            AppDraw.TextColored(Palette32.OrangeBase, "Fallback"u8);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(profile.ToUtf8());
        ImGui.EndGroup();

        var availWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SameLine(availWidth - ImageOffset);

        AppDraw.ImageButton("##img"u8, id, ref _textureHandles[slot], new Vector2(ImageThumbnailSize));
    }
}

internal sealed class TextureSlotField : InputField
{
    const float ImageThumbnailSize = 64f;
    private static float ImageOffset => ImageThumbnailSize + GuiTheme.FramePadding.X;

    private int _count;
    private readonly AssetId[] _textureIds = new AssetId[16];
    private readonly TexturePtrHandle[] _textureHandles = new TexturePtrHandle[16];

    public TextureSlotField(string name) : base(name, InputKind.Texture) { }

    public void SetValue(ReadOnlySpan<AssetId> textureIds)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(textureIds.Length, _textureIds.Length);
        textureIds.CopyTo(_textureIds);
        _count = textureIds.Length;
    }

    public override bool Draw()
    {
        var ids = _textureIds.AsSpan(0, _count);
        for (var i = 0; i < ids.Length; i++)
        {
            var assetId = ids[i];
            if (!assetId.IsValid()) Throwers.InvalidOperation(nameof(assetId));
            var texture = AssetManager.Assets.Get<Texture>(ids[i]);
            DrawSlot(i, texture.Name, texture.GfxId, texture.TextureKind, texture.Profile);
        }

        return false;
    }


    private void DrawSlot(int slot, string name, TextureId id, TextureKind kind, SamplerProfile profile)
    {
        ImGui.BeginGroup();
        if (id.IsValid())
        {
            ImGui.AlignTextToFramePadding();
            AppDraw.Text(name);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(kind.ToUtf8());
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            AppDraw.TextColored(Palette32.OrangeBase, "Fallback"u8);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(profile.ToUtf8());
        ImGui.EndGroup();

        var availWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SameLine(availWidth - ImageOffset);

        AppDraw.ImageButton("##img"u8, id, ref _textureHandles[slot], new Vector2(ImageThumbnailSize));
    }
}
*/
internal sealed unsafe class MaterialBindingForm
{
    const float ImageThumbnailSize = 64f;

    private readonly TexturePtrHandle[] _textureHandles = new TexturePtrHandle[16];

    private static float ImageOffset => ImageThumbnailSize + GuiTheme.FramePadding.X;

    public void Draw(Material material)
    {
        var span = material.GetSourceSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var source = span[i];
            ImGui.PushID(i);

            ImGui.SeparatorText(source.Slot.ToUtf8());

            DrawTexture(source, ref _textureHandles[i]);

            if (source.TextureId.IsValid() && ImGui.IsItemClicked(ImGuiMouseButton.Right))
                material.ClearSourceSlot(source.Slot);

            DropTexture(material, source.Slot);


            ImGui.PopID();
        }
    }

    private static void DropTexture(Material material, SamplerSlot slot)
    {
        if (!ImGui.BeginDragDropTarget()) return;

        var payload = ImGui.AcceptDragDropPayload("ASSET_TEXTURE"u8);
        if (!payload.IsNull && payload.IsDelivery())
        {
            var droppedId = *(AssetId*)payload.Data;
            if (droppedId.Id > 0 && AssetManager.Assets.TryGet<Texture>(droppedId, out var droppedTex))
                material.SetSourceSlot(droppedTex, slot);
        }

        ImGui.EndDragDropTarget();
    }

    public static void DrawTexture(TextureSource source, ref TexturePtrHandle handle)
    {
        ImGui.BeginGroup();
        if (source.TextureId.IsValid())
        {
            var texture = AssetManager.Assets.Get<Texture>(source.AssetId);
            ImGui.AlignTextToFramePadding();
            AppDraw.Text(texture.Name);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(texture.TextureKind.ToUtf8());
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            AppDraw.TextColored(Palette32.OrangeBase, "Fallback"u8);
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(source.Profile.ToUtf8());
        ImGui.EndGroup();

        var availWidth = ImGui.GetContentRegionAvail().X;
        ImGui.SameLine(availWidth - ImageOffset);

        AppDraw.ImageButton("##img"u8, source.GetTextureOrFallback(), ref handle, new Vector2(ImageThumbnailSize));
    }
}