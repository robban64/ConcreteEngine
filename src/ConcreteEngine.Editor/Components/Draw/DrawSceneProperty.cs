using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components.Draw;

internal static class DrawSceneProperty
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    public static void DrawInfo(DrawContext draw, SceneObjectProxy selection)
    {
        var write = draw.GetWriter();
        draw.DrawRightProp(ref write.Append(selection.Name), "Name:"u8);
        draw.DrawRightProp(ref write.Append(selection.GIdString), "GID:"u8);

        ImGui.Dummy(new Vector2(0, 2));
    }

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


    public static void DrawRenderProperty(DrawContext draw, ProxyPropertyEntry<SourceProperty> prop)
    {
        var value = prop.Get();
        var writer = draw.GetWriter();

        draw.DrawRightProp(ref writer.AppendEnd(value.Mesh), "Mesh:"u8);
        ImGui.Dummy(new Vector2(0, 2));
        draw.DrawRightProp(ref writer.AppendEnd(value.MaterialId), "Material:"u8);
    }

    public static void DrawParticleProperty(DrawContext draw, SceneState sceneState)
    {
        var fieldStatus = new FormFieldStatus();

        ImGui.SeparatorText("Particle Component"u8);

        var writer = draw.GetWriter();
        writer.Clear();
        draw.DrawRightProp(ref writer.AppendEnd(sceneState.Particle.EmitterHandle), "ID:"u8);

        ref var def = ref sceneState.Particle.Definition;
        ref var state = ref sceneState.Particle.State;
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

    public static void DrawAnimationProperty(SceneState state, ZaUtf8SpanWriter za)
    {
        ref var animation = ref state.Animation;
        var fieldStatus = new FormFieldStatus();
        ImGui.SeparatorText("Animation Component"u8);

        ImGui.TextUnformatted("ID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(animation.Animation).AsSpan());

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted("Clip - Length: "u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(animation.ClipCount).AsSpan());
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