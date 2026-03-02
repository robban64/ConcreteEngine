using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Panels.Scene;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed unsafe class ScenePropertyPanel(StateContext context) : EditorPanel(PanelId.SceneProperty, context)
{
    private const ImGuiInputTextFlags InputFlags = ImGuiInputTextFlags.EnterReturnsTrue |
                                                   ImGuiInputTextFlags.CharsNoBlank |
                                                   ImGuiInputTextFlags.CallbackCharFilter;

    private static readonly char[] ValidNoneAlphaNumericChars = ['_', '-'];

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
        GuiTheme.PushFontIconText();
        if (ImGui.Button(ctx.Sw.Write(IconNames.Undo2)))
            RestoreName(inspector.SceneObject);
        ImGui.PopFont();

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, ref _nameBuffer.GetRef(), String64Utf8.Capacity, InputFlags, InputCallback))
        {
        }

        ImGui.EndGroup();

        ImGui.Spacing();
        DrawProperties(inspector, ctx);
        /*
        AppDraw.DrawTextProperty("Mesh:"u8, ctx.Sw.Write(props.SourceProperty.Mesh));
        ImGui.Spacing();
        AppDraw.DrawTextProperty("Material:"u8, ctx.Sw.Write(props.SourceProperty.MaterialId));

        DrawSceneProperty.DrawTransform(props.SpatialProperty);

        if (props.ParticleProperty is { } particle)
            DrawSceneProperty.DrawParticleProperty(particle, ctx);

        if (props.AnimationProperty is { } animation)
            DrawSceneProperty.DrawAnimationProperty(animation, ctx);
        */
    }

    private void DrawProperties(SceneObjectInspector inspector, FrameContext ctx)
    {
        if (ImGui.CollapsingHeader("Transform"))
        {
            inspector.TranslationField.Draw();
            inspector.ScaleField.Draw();
            inspector.RotationField.Draw();
        }

        if (inspector.AnimationFields is { } animation && ImGui.CollapsingHeader("Animation"))
        {
            ImGui.TextUnformatted("Clips: "u8);
            ImGui.SameLine();
            ImGui.TextUnformatted("asd"u8);
            //ImGui.TextUnformatted(ctx.Sw.Write(prop.ClipCount));

            animation.SpeedField.Draw();
            animation.DurationField.Draw();
        }


        if (inspector.ParticleFields is { } particle && ImGui.CollapsingHeader("Particle"))
        {
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
}