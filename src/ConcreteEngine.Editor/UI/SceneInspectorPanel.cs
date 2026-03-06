using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
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

    [FixedAddressValueType]
    private static String64Utf8 _nameBuffer;

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

    private static void RestoreName(SceneObject sceneObject) => _nameBuffer = new String64Utf8(sceneObject.Name);

    private SceneObjectId _previousId = SceneObjectId.Empty;

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
        {
            RestoreName(inspector.SceneObject);
            _previousId = inspector.Id;
        }

        //
        ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetSceneColor(inspector.Kind));
        ImGui.SeparatorText(ref ctx.Sw.Append(inspector.Kind.ToText()).Append(" - ["u8).Append(inspector.Id).Append(']')
            .End());
        ImGui.PopStyleColor();

        //
        ImGui.BeginGroup();
        //GuiTheme.PushFontIconSmall();
        if (ImGui.Button(ctx.Sw.Write(IconNames.Undo2)))
        {
            RestoreName(inspector.SceneObject);
        }

        //ImGui.PopFont();

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, ref _nameBuffer.GetRef(), 64, GuiTheme.InputNameFlags, InputCallback))
        {
        }

        ImGui.EndGroup();

        ImGui.Spacing();
        DrawProperties(inspector, ctx);
    }

    private static void DrawProperties(InspectSceneObject inspector, FrameContext ctx)
    {
        ImGui.PushItemWidth(float.Min(GuiTheme.FormItemWidth, ImGui.GetContentRegionAvail().X));
        ImGui.Spacing();
        ImGui.Separator();
        if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
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
            if (ImGui.CollapsingHeader("Animation", ImGuiTreeNodeFlags.DefaultOpen))
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
            if (ImGui.CollapsingHeader("Particle", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var particle = inspector.ParticleFields;
                ImGui.Spacing();
                ImGui.SeparatorText("Definition"u8);
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
}