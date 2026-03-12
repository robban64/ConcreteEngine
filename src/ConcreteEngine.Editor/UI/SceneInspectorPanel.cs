using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class SceneInspectorPanel(StateContext context) : EditorPanel(PanelId.SceneInspector, context)
{
    private static readonly char[] ValidNoneAlphaNumericChars = ['_', '-'];

    [FixedAddressValueType] private static String64Utf8 _nameBuffer;
    private static void RestoreName(SceneObject sceneObject) => _nameBuffer = new String64Utf8(sceneObject.Name);


    private NativeViewPtr<byte> _titleStrPtr = TextBuffers.Arena.Alloc(24);

    private SceneObjectId _previousId = SceneObjectId.Empty;

    private void OnNewInspector(InspectSceneObject inspector)
    {
        RestoreName(inspector.SceneObject);
        _previousId = inspector.Id;

        _titleStrPtr.Writer().Append(inspector.Kind.ToText()).Append(" - ["u8).Append(inspector.Id).Append(']').End();
    }

    public override void Enter()
    {
        if (Context.Selection.SelectedSceneObject is not { } inspector) return;
        inspector.TranslationField.Refresh();
        inspector.ScaleField.Refresh();
        inspector.RotationField.Refresh();
    }

    public override void Draw(FrameContext ctx)
    {
        if (Context.Selection.SelectedSceneObject is not { } inspector) return;

        if (_previousId != inspector.Id)
            OnNewInspector(inspector);

        //
        ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetSceneColor(inspector.Kind));
        ImGui.SeparatorText(_titleStrPtr);
        ImGui.PopStyleColor();

        //
        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(Icons.Undo2)))
            RestoreName(inspector.SceneObject);

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, ref _nameBuffer.GetRef(), 64, GuiTheme.InputNameFlags, InputCallback))
            HandleRename(inspector);

        ImGui.EndGroup();

        ImGui.Spacing();
        DrawProperties(inspector, ctx);
    }

    private static void DrawProperties(InspectSceneObject inspector, FrameContext ctx)
    {
        ImGui.PushItemWidth(float.Min(GuiTheme.FormItemWidth, ImGui.GetContentRegionAvail().X));
        ImGui.Spacing();
        ImGui.Separator();
        if (ImGui.CollapsingHeader("Transform"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Spacing();
            inspector.TranslationField.Draw();
            inspector.ScaleField.Draw();
            inspector.RotationField.Draw();
        }

        ImGui.Spacing();
        ImGui.Separator();
        if (inspector.AnimationFields != null)
        {
            ImGui.Spacing();
            if (ImGui.CollapsingHeader("Animation"u8, ImGuiTreeNodeFlags.DefaultOpen))
            {
                var animation = inspector.AnimationFields;

                ImGui.TextUnformatted("Clips: "u8);
                ImGui.SameLine();
                ImGui.TextUnformatted("asd"u8);
                //ImGui.TextUnformatted(ctx.Sw.Write(prop.ClipCount));

                animation.SpeedField.Draw();
                animation.DurationField.Draw();
            }
        }

        if (inspector.ParticleFields != null)
        {
            ImGui.Spacing();
            ImGui.Separator();
            if (ImGui.CollapsingHeader("Particle"u8, ImGuiTreeNodeFlags.DefaultOpen))
            {
                var particle = inspector.ParticleFields;
                ImGui.Spacing();
                particle.StartColorField.Draw();
                particle.EndColorField.Draw();
                particle.SizeStartEndField.Draw();
                particle.GravityField.Draw();
                particle.DragField.Draw();
                particle.SpeedMinMaxField.Draw();
                particle.LifeMinMaxField.Draw();

                ImGui.Spacing();
                ImGui.SeparatorText("State"u8);
                particle.TranslationField.Draw();
                particle.StartAreaField.Draw();
                particle.DirectionField.Draw();
                particle.SpreadField.Draw();
            }
        }

        ImGui.PopItemWidth();
    }

    private static void HandleRename(InspectSceneObject inspect)
    {
        UtfText.SliceNullTerminate(_nameBuffer.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;
        if (!UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        chars = chars.Trim();
        if (chars.IsEmpty || chars.Equals(inspect.SceneObject.Name, StringComparison.Ordinal)) return;

        var name = chars.ToString();
        inspect.SceneObject.SetName(name);
        // Context.EnqueueEvent(new AssetUpdateEvent(AssetUpdateEvent.EventAction.Rename, inspectAsset.Id, name));
    }

    private static int InputCallback(ImGuiInputTextCallbackData* data)
    {
        if (data->EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
        {
            var c = (char)data->EventChar;
            if (char.IsAsciiDigit(c) || char.IsAsciiLetterOrDigit(c)) return 0;
            if (ValidNoneAlphaNumericChars.AsSpan().Contains(c)) return 0;
            return 1;
        }

        return 0;
    }
}