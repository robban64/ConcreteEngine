using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;

namespace ConcreteEngine.Editor.Components.Draw;

internal static class DrawSceneProperty
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    public static void DrawInfo(SceneObjectProxy selection, ref ZaUtf8SpanWriter za)
    {
        ImGui.TextUnformatted("Name:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(selection.Name).AsSpan());
        za.Clear();

        ImGui.TextUnformatted("GID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(selection.GIdString).AsSpan().Slice(0, 16));
        za.Clear();

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.Separator();
        ImGui.TextUnformatted("Model:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted("0"u8);

        ImGui.TextUnformatted("Material:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted("0"u8);
    }

    public static void DrawTransform(SceneState state, ProxyPropertyEntry<SpatialProperty> prop)
    {
        ref var transform = ref state.Transform;
        var fieldStatus = new ImGuiFieldStatus();

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform"u8);

        ImGui.TextUnformatted("Translation"u8);
        ImGui.Separator();
        ImGui.InputFloat3("##ent-prop-translation", ref transform.Translation, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Scale"u8);
        ImGui.Separator();
        ImGui.InputFloat3("##ent-prop-scale", ref transform.Scale, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation"u8);
        ImGui.Separator();
        ImGui.InputFloat3("##ent-prop-rotation", ref transform.EulerAngles, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        if (fieldStatus.HasEdited(out _))
        {
            transform.FillTransform(out var result);
            prop.InvokeSet(new SpatialProperty(in result, in prop.Get().Bounds));
        }
    }


    public static void DrawRenderProperty(ProxyPropertyEntry<SourceProperty> prop, ref ZaUtf8SpanWriter za)
    {
        var value = prop.Get();
        ImGui.TextUnformatted("Model:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(value.Model).AsSpan());
        za.Clear();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted(za.AppendEnd(value.MaterialKey).AsSpan());
        za.Clear();
    }

    public static void DrawParticleProperty(SceneState sceneState, ref ZaUtf8SpanWriter za)
    {
        ref var particle = ref sceneState.Particle;
        ref var def = ref particle.Definition;
        ref var state = ref particle.EmitterState;

        var fieldStatus = new ImGuiFieldStatus();

        ImGui.SeparatorText("Particle Component"u8);

        ImGui.TextUnformatted("ID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(particle.EmitterHandle).AsSpan());

        //DEF
        ImGui.SeparatorText("Definition"u8);

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Start Color"u8);
        ImGui.ColorEdit4("##start-color", ref def.StartColor);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("End Color"u8);
        ImGui.ColorEdit4("##end-color", ref def.EndColor);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Size Start / End"u8);
        ImGui.InputFloat2("##size-start-end", ref def.SizeStartEnd);
        fieldStatus.NextField();

        ImGui.Separator();

        ImGui.TextUnformatted("Gravity"u8);
        ImGui.InputFloat3("##gravity", ref def.Gravity);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Drag"u8);
        ImGui.InputFloat("##drag"u8, ref def.Drag);
        fieldStatus.NextField();

        ImGui.Separator();

        ImGui.TextUnformatted("Speed Min / Max"u8);
        ImGui.InputFloat2("##speed-min-max", ref def.SpeedMinMax);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Life Min / Max"u8);
        ImGui.InputFloat2("##life-min-max", ref def.LifeMinMax);
        fieldStatus.NextField();
        ImGui.EndGroup();

        //STATE
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Translation"u8);
        ImGui.InputFloat3("##translation", ref state.Translation);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Start Area"u8);
        ImGui.InputFloat3("##start-area", ref state.StartArea);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Direction"u8);

        ImGui.InputFloat3("##direction", ref state.Direction);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Spread"u8);
        ImGui.InputFloat("##spread"u8, ref state.Spread);
        fieldStatus.NextField();
        ImGui.EndGroup();


        if (fieldStatus.HasEdited(out _))
        {
        }
    }

    public static void DrawAnimationProperty(SceneState state, ref ZaUtf8SpanWriter za)
    {
        ref var animation = ref state.Animation;
        var fieldStatus = new ImGuiFieldStatus();
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