using System.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Controller;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Scene;

internal static class DrawSceneProperty
{
    public static void DrawTransform(SpatialProperty prop)
    {
        ImGui.PushID("transform-prop"u8);

        ImGui.Dummy(TextLayout.DefaultVSpace);
        ImGui.SeparatorText("Transform"u8);

        var fieldStatus = FormFieldInputs.MakeVertical();

        ref var transform = ref prop.Transform;
        fieldStatus.InputFloat("Translation"u8, InputComponents.Float3, ref transform.Translation.X);
        fieldStatus.InputFloat("Scale"u8, InputComponents.Float3, ref transform.Scale.X);
        fieldStatus.InputFloat("Rotation"u8, InputComponents.Float3, ref transform.EulerAngles.X);

        if (fieldStatus.HasEdited(out _))
        {
            prop.InvokeSet();
        }

        ImGui.PopID();
    }

    public static void DrawParticleProperty(ParticleProperty prop, UnsafeSpanWriter sw)
    {
        ImGui.PushID("particle-form"u8);

        TextLayout.Make().TitleSeparator("Particle Component"u8)
            .Property("ID:"u8, ref sw.Write(prop.EmitterHandle));

        var fieldStatus = FormFieldInputs.MakeVertical();

        //DEF
        ImGui.SeparatorText("Definition"u8);
        ImGui.BeginGroup();
        {
            ref var def = ref prop.Definition;
            fieldStatus.ColorEdit4("Start Color"u8, ref def.StartColor.X);
            fieldStatus.ColorEdit4("End Color"u8, ref def.EndColor.X);
            fieldStatus.InputFloat("Size Start / End"u8, InputComponents.Float2, ref def.SizeStartEnd.X, "%.3f");
            ImGui.Separator();
            fieldStatus.InputFloat("Gravity"u8, InputComponents.Float3, ref def.Gravity.X, "%.3f");
            fieldStatus.InputFloat("Drag"u8, InputComponents.Float1, ref def.Drag, "%.3f");
            ImGui.Separator();
            fieldStatus.InputFloat("Speed Min / Max"u8, InputComponents.Float2, ref def.SpeedMinMax.X, "%.3f");
            fieldStatus.InputFloat("Life Min / Max"u8, InputComponents.Float2, ref def.LifeMinMax.X, "%.3f");
        }
        ImGui.EndGroup();

        //STATE
        ImGui.SeparatorText("State"u8);
        ImGui.BeginGroup();
        {
            ref var state = ref prop.State;
            fieldStatus.InputFloat("Translation"u8, InputComponents.Float3, ref state.Translation.X, "%.3f");
            fieldStatus.InputFloat("Start Area"u8, InputComponents.Float3, ref state.StartArea.X, "%.3f");
            fieldStatus.InputFloat("Direction"u8, InputComponents.Float3, ref state.Direction.X, "%.3f");
            fieldStatus.InputFloat("Spread"u8, InputComponents.Float1, ref state.Spread, "%.3f");
        }
        ImGui.EndGroup();
        if (fieldStatus.HasEdited(out _))
        {
        }

        ImGui.PopID();
    }

    public static void DrawAnimationProperty(AnimationProperty prop, in FrameContext ctx)
    {
        ImGui.SeparatorText("Animation Component"u8);

        ImGui.TextUnformatted("ID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(ref ctx.Writer.Write(prop.Animation));

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted("Clip - Length: "u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(ref ctx.Writer.Write(prop.ClipCount));
        ImGui.Separator();
        if (ImGui.InputInt("##ani-prop-clip"u8, ref prop.Clip, 1))
            prop.Clip = int.Clamp(prop.Clip, 0, prop.ClipCount - 1);

        ImGui.TextUnformatted("Speed"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ani-speed"u8, ref prop.Speed);

        ImGui.TextUnformatted("Duration"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-dura"u8, ref prop.Duration);

        ImGui.TextUnformatted("Time"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-time"u8, ref prop.Time);
    }
}