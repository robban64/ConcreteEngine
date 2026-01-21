using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Panels.State;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels.Scene;

internal static class DrawSceneProperty
{
    public static void DrawTransform( SpatialProperty prop)
    {
        ref var transform = ref prop.Transform;
        var fieldStatus = new FormFieldStatus(useTopLabel:true);

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform"u8);

        fieldStatus.InputFloat3("Translation"u8, "##ent-t-t", ref transform.Translation);
        fieldStatus.InputFloat3("Scale"u8, "##ent-t-s", ref transform.Scale);
        fieldStatus.InputFloat3("Translation"u8, "##ent-t-r", ref transform.EulerAngles);

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
        var fieldStatus = new FormFieldStatus(useTopLabel:true);

        ImGui.SeparatorText("Particle Component"u8);

        TextLayout.Make().Property("ID:"u8, ctx.Sw.Write(prop.EmitterHandle));

        ref var def = ref prop.Definition;
        ref var state = ref prop.State;
        //DEF
        ImGui.SeparatorText("Definition"u8);
        ImGui.BeginGroup();
        fieldStatus.ColorEdit4("Start Color"u8, "##s-color", ref def.StartColor);
        fieldStatus.ColorEdit4("End Color"u8, "##e-color", ref def.EndColor);
        fieldStatus.InputFloat2("Size Start / End"u8, "##size-se", ref def.SizeStartEnd);

        ImGui.Separator();

        fieldStatus.InputFloat3("Gravity"u8, "##gvt", ref def.Gravity);
        fieldStatus.InputFloat("Drag"u8, "##drag", ref def.Drag);

        ImGui.Separator();

        fieldStatus.InputFloat2("Speed Min / Max"u8, "##s-mm", ref def.SpeedMinMax);
        fieldStatus.InputFloat2("Life Min / Max"u8, "##l-mm", ref def.LifeMinMax);
        ImGui.EndGroup();

        //STATE
        ImGui.SeparatorText("State"u8);
        ImGui.BeginGroup();
        fieldStatus.InputFloat3("Translation"u8, "##p-trans", ref state.Translation);
        fieldStatus.InputFloat3("Start Area"u8, "##p-sa", ref state.StartArea);

        fieldStatus.InputFloat3("Direction"u8, "##p-dir", ref state.Direction);
        fieldStatus.InputFloat("Spread"u8, "##p-spr", ref state.Spread);
        ImGui.EndGroup();


        if (fieldStatus.HasEdited(out _))
        {
        }
    }

    public static void DrawAnimationProperty(AnimationProperty prop, ref FrameContext ctx)
    {
        var fieldStatus = new FormFieldStatus(useTopLabel:true);
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

        fieldStatus.NextField();

        ImGui.TextUnformatted("Speed"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ani-speed"u8, ref prop.Speed);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Duration"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-dura"u8, ref prop.Duration);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Time"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-time"u8, ref prop.Time);
        fieldStatus.NextField();

        if (fieldStatus.HasEdited(out _))
        {
        }
    }
}