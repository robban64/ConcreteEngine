using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Inspectors;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets;

[EditorInspector(typeof(Texture))]
internal sealed partial class TextureInspector : Inspector<TextureInspector>
{
    public static Texture Target => (Texture)SelectionManager.Instance.SelectedAsset!;
    
    public override uint Icon => IconNames.Image;

    private static TexturePtrHandle _imageHandle;

    public unsafe void Draw()
    {
        AppDraw.Section("Preview"u8, &DrawImage);
        AppDraw.Section("Info"u8, &DrawInfo);
    }

    private static void DrawImage()
    {
        ref var imageHandle = ref _imageHandle;
        if (!ImGuiSystem.TryResolveTextureHandle(Target.GfxId, ref imageHandle)) return;

        var contentWidth = ImGui.GetContentRegionAvail().X;

        var imageSize = float.Clamp(contentWidth, 32f, 256f);
        imageSize -= GuiTheme.FramePadding.X * 2f;

        var text = ScratchBuffer.Writer().Write(Target.Size);
        ImGui.AlignTextToFramePadding();
        AppDraw.Text(text);
        ImGui.SameLine();
        ImGui.TextUnformatted(Target.PixelFormat.ToUtf8());

        ImGui.Spacing();
        
        ImGui.ImageButton("##x"u8, imageHandle, new Vector2(imageSize));
    }

    private static void DrawInfo()
    {
        var texture = Target;
        var offset = ImGui.GetContentRegionAvail().X / 2f;

        AppDraw.TextPropertyField(offset, "Kind"u8, texture.Kind.ToUtf8());
        AppDraw.TextPropertyField(offset, "Profile"u8, texture.Profile.ToUtf8());
        AppDraw.TextPropertyField(offset, "InMemory"u8, texture.HasPixelData ? "Yes"u8 : "No"u8);


        var sw = ScratchBuffer.Writer();
        var mips =  GfxRegistry.GetMeta(texture.GfxId).MipLevels;
        AppDraw.TextPropertyField(offset, "Mips"u8, sw.Write(mips));
    }

}