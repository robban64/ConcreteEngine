using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Panels.State;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Scene;

internal static class DrawSceneProperty
{
    public static void DrawTransform(SpatialProperty prop)
    {
        ref var transform = ref prop.Transform;
        var fieldStatus = new FormFieldInputs(topLabel: true, width: 0);

        if(!ImGui.BeginChild("##transform-prop")) return;

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform"u8);

        fieldStatus.InputFloat("Translation"u8, InputComponents.Float3, ref transform.Translation.X);
        fieldStatus.InputFloat("Scale"u8, InputComponents.Float3, ref transform.Scale.X);
        fieldStatus.InputFloat("Rotation"u8, InputComponents.Float3, ref transform.EulerAngles.X);
        ImGui.EndChild();

        if (fieldStatus.HasEdited(out _))
        {
            prop.InvokeSet();
        }
    }


    public static void DrawRenderProperty(SourceProperty prop, ref FrameContext ctx)
    {
        TextLayout.Make()
            .Property("Mesh:"u8, ctx.Sw.Write(prop.Mesh.Value)).RowSpace()
            .Property("Material:"u8, ctx.Sw.Write(prop.MaterialId.Id));
    }

    public static void DrawParticleProperty(ParticleProperty prop, ref FrameContext ctx)
    {
        if(!ImGui.BeginChild("##particle-prop")) return;
        ImGui.SeparatorText("Particle Component"u8);

        TextLayout.Make().Property("ID:"u8, ctx.Sw.Write(prop.EmitterHandle));

        ref var def = ref prop.Definition;
        ref var state = ref prop.State;

        var fieldStatus = new FormFieldInputs(topLabel: true, width: 0);
        //DEF
        ImGui.SeparatorText("Definition"u8);
        ImGui.BeginGroup();
        fieldStatus.ColorEdit4("Start Color"u8, ref def.StartColor.X);
        fieldStatus.ColorEdit4("End Color"u8, ref def.EndColor.X);
        fieldStatus.InputFloat("Size Start / End"u8, InputComponents.Float2, ref def.SizeStartEnd.X, "%.3f");

        ImGui.Separator();

        fieldStatus.InputFloat("Gravity"u8, InputComponents.Float3, ref def.Gravity.X, "%.3f");
        fieldStatus.InputFloat("Drag"u8, InputComponents.Float1, ref def.Drag, "%.3f");

        ImGui.Separator();

        fieldStatus.InputFloat("Speed Min / Max"u8, InputComponents.Float2, ref def.SpeedMinMax.X, "%.3f");
        fieldStatus.InputFloat("Life Min / Max"u8, InputComponents.Float2, ref def.LifeMinMax.X, "%.3f");
        ImGui.EndGroup();

        //STATE
        ImGui.SeparatorText("State"u8);
        ImGui.BeginGroup();
        fieldStatus.InputFloat("Translation"u8, InputComponents.Float3, ref state.Translation.X, "%.3f");
        fieldStatus.InputFloat("Start Area"u8, InputComponents.Float3, ref state.StartArea.X, "%.3f");

        fieldStatus.InputFloat("Direction"u8, InputComponents.Float3, ref state.Direction.X, "%.3f");
        fieldStatus.InputFloat("Spread"u8, InputComponents.Float1, ref state.Spread, "%.3f");
        ImGui.EndGroup();
        ImGui.EndChild();
        if (fieldStatus.HasEdited(out _))
        {
        }
    }

    public static void DrawAnimationProperty(AnimationProperty prop, ref FrameContext ctx)
    {
        ImGui.SeparatorText("Animation Component"u8);

        ImGui.TextUnformatted("ID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(ctx.Sw.Write(prop.Animation.Value));

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted("Clip - Length: "u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(ctx.Sw.Write(prop.ClipCount));
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