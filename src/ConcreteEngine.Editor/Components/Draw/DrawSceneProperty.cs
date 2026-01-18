using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Components.State;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Components.Draw;

internal static class DrawSceneProperty
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;


    public static void DrawTransform(SceneState state, ProxyPropertyEntry<SpatialProperty> prop)
    {
        ref var transform = ref state.Transform;
        var fieldStatus = new FormFieldStatus();

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform"u8);

        fieldStatus.InputFloat3("Translation"u8, "##ent-t-t", ref transform.Translation);
        fieldStatus.InputFloat3("Scale"u8, "##ent-t-s", ref transform.Scale);
        fieldStatus.InputFloat3("Translation"u8, "##ent-t-r", ref transform.EulerAngles);

        if (fieldStatus.HasEdited(out _))
        {
            transform.FillTransform(out var result);
            prop.InvokeSet(new SpatialProperty(in result, in prop.Get().Bounds));
        }
    }


    public static void DrawRenderProperty(ProxyPropertyEntry<SourceProperty> prop, ref FrameContext ctx)
    {
        var value = prop.Get();
        TextLayout.Make()
            .Property("Mesh:"u8, ctx.Sw.Write(value.Mesh.Value)).RowSpace()
            .Property("Material:"u8, ctx.Sw.Write(value.MaterialId.Id));
    }

    public static void DrawParticleProperty(SceneState sceneState, ref FrameContext ctx)
    {
        var particle = sceneState.Particle;
        var fieldStatus = new FormFieldStatus();

        ImGui.SeparatorText("Particle Component"u8);

        TextLayout.Make().Property("ID:"u8, ctx.Sw.Write(particle.EmitterHandle));

        ref var def = ref particle.Definition;
        ref var state = ref particle.State;
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

    public static void DrawAnimationProperty(SceneState state, ref FrameContext ctx)
    {
        ref var animation = ref state.Animation;
        var fieldStatus = new FormFieldStatus();
        ImGui.SeparatorText("Animation Component"u8);

        ImGui.TextUnformatted("ID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(ctx.Sw.Write(animation.Animation.Value));

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted("Clip - Length: "u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(ctx.Sw.Write(animation.ClipCount));
        ImGui.Separator();
        if (ImGui.InputInt("##ani-prop-clip"u8, ref animation.Clip, 1))
            animation.Clip = int.Clamp(animation.Clip, 0, animation.ClipCount - 1);

        fieldStatus.NextField();

        ImGui.TextUnformatted("Speed"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ani-speed"u8, ref animation.Speed);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Duration"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-dura"u8, ref animation.Duration);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Time"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-time"u8, ref animation.Time);
        fieldStatus.NextField();

        if (fieldStatus.HasEdited(out _))
        {
        }
    }
}