using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Assets.Components;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets;

[EditorInspector(typeof(Material))]
internal sealed partial class MaterialInspector : Inspector<MaterialInspector>
{
    public static Material Target => (Material)SelectionManager.Instance.SelectedAsset!;
    
    private readonly MaterialBindingForm _bindingForm = new();

    public unsafe void Draw()
    {
        DrawHeader();
        AppDraw.CollapseSection("Bindings"u8, &DrawBindings);
        AppDraw.CollapseSection("State"u8, &DrawState);
        AppDraw.CollapseSection("Rendering"u8, &DrawPipeline);
    }

    private static void DrawHeader()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Shader: "u8);
        ImGui.SameLine();
        AppDraw.TextColored(Palette32.White, Target.BoundShader.Name);
    }

    private static void DrawBindings() => Instance._bindingForm.Draw(Target);

    private static void DrawPipeline()
    {
        Instance.DrawRenderFlags();
        Instance.DrawRenderCombos();
    }
    
    
    private readonly ComboInput BlendCombo = ComboInput.Create("Blend Mode", BlendModeExt.Values, BlendModeExt.Names,
        static v => Target.State.DrawFunctions = Target.State.DrawFunctions with { Blend = (BlendMode)v });

    private readonly ComboInput CullCombo = ComboInput.Create("Cull Mode", CullModeExt.Values, CullModeExt.Names,
        static v => Target.State.DrawFunctions = Target.State.DrawFunctions with { Cull = (CullMode)v });

    private readonly ComboInput DepthCombo = ComboInput.Create("Depth Mode", DepthModeExt.Values, DepthModeExt.Names,
        static v => Target.State.DrawFunctions = Target.State.DrawFunctions with { Depth = (DepthMode)v });

    private readonly ComboInput PolygonCombo = ComboInput.Create("Polygon Offset",
        PolygonOffsetLevelExt.Values, PolygonOffsetLevelExt.Names,
        static v => Target.State.DrawFunctions = Target.State.DrawFunctions with
        {
            PolygonOffset = (PolygonOffsetLevel)v
        });

    private void DrawRenderCombos()
    {
        var drawFunc = Target.State.DrawFunctions;
        BlendCombo.Value = (int)drawFunc.Blend;
        CullCombo.Value = (int)drawFunc.Cull;
        DepthCombo.Value = (int)drawFunc.Depth;
        PolygonCombo.Value = (int)drawFunc.PolygonOffset;

        BlendCombo.Draw();
        CullCombo.Draw();
        DepthCombo.Draw();
        PolygonCombo.Draw();
    }
    
    private unsafe void DrawRenderFlags()
    {
        var ogDrawState = Target.State.DrawState;
        var drawState = ogDrawState;
        DrawFlagToggle("Blend Mode"u8, GfxDrawFlags.Blend, ref drawState);
        DrawFlagToggle("Cull Mode"u8, GfxDrawFlags.Cull, ref drawState);
        DrawFlagToggle("Depth Test"u8, GfxDrawFlags.DepthTest, ref drawState);
        DrawFlagToggle("Depth Write"u8, GfxDrawFlags.DepthWrite, ref drawState);
        DrawFlagToggle("Polygon Offset"u8, GfxDrawFlags.PolygonOffset, ref drawState);
        ImGui.Separator();
        DrawFlagToggle("A2C"u8, GfxDrawFlags.Ac2, ref drawState);

        if (ogDrawState != drawState)
            Target.State.DrawState = drawState;
        return;

        static void DrawFlagToggle(ReadOnlySpan<byte> label, GfxDrawFlags flag, ref GfxDrawState state)
        {
            var isDefined = state.IsSet(flag);

            var sw = ScratchBuffer.Writer();
            sw.Append(label);
            if (ImGui.Checkbox(sw.AppendImGuiId(1).Append((byte)flag).End(), ref isDefined))
                state = new GfxDrawState(state.Enabled, state.Defined ^ flag);

            if (!isDefined) return;

            ImGui.SameLine(130);

            var isEnabled = state.IsEnabled(flag);
            if (ImGui.Checkbox(sw.AppendImGuiId(2).Append((byte)flag).End(), ref isEnabled))
                state = new GfxDrawState(state.Enabled ^ flag, state.Defined);
        }
    }

}