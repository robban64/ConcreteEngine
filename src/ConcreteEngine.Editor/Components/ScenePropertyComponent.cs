using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer.Data;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components;

internal static class ScenePropertyComponent
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    private static SceneObjectProxy? Selection => StoreHub.SelectedProxy;


    public static void Draw(EmptyState state)
    {
        if (!StoreHub.SelectedId.IsValid() || Selection == null) return;

        Span<byte> buffer = stackalloc byte[64];
        var za = ZaUtf8SpanWriter.Create(buffer);
        var selection = Selection;

        float childHeight = ImGui.GetContentRegionAvail().Y - 2;
        if (ImGui.BeginChild("##right-sidebar-properties"u8, new Vector2(0, childHeight),
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
        {
            ImGui.SeparatorText(za.Append("Scene Object ["u8).Append(selection.Id).AppendEnd(")"u8).AsSpan());
            za.Clear();
            DrawInfo(selection, ref za);
            DrawTransform(selection.GetSpatialProperty());
            foreach (var property in selection.Properties)
            {
                switch (property)
                {
                    case ProxyPropertyEntry<SourceProperty> renderProp: DrawRenderProperty(renderProp, ref za); break;
                    case ProxyPropertyEntry<ParticleProperty> partProp: DrawParticleProperty(partProp, ref za); break;
                    case ProxyPropertyEntry<AnimationProperty> animProp: DrawAnimationProperty(animProp, ref za); break;
                }
            }

            /*
            var componentRef = EditorDataStore.EntityState.ComponentRef;
            if (!componentRef.IsValid)
            {
                ImGui.EndChild();
                return;
            }

            ImGui.Dummy(new Vector2(0, 4));

            if (componentRef.ItemType == EditorItemType.Animation)
                DrawAnimationProperties();
            else if (componentRef.ItemType == EditorItemType.Particle)
                DrawParticleProperties();
                */
            ImGui.EndChild();
        }
    }


    private static void DrawInfo(SceneObjectProxy selection, ref ZaUtf8SpanWriter za)
    {
        ImGui.TextUnformatted("Name:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(selection.Name).AsSpan());
        za.Clear();

        ImGui.TextUnformatted("GID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(selection.GIdString).AsSpan());
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

    private static void DrawTransform(ProxyPropertyEntry<SpatialProperty> prop)
    {
        var value = prop.GetValue();
        ref var transform = ref value.Transform;
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
        //ImGui.InputFloat3("##ent-prop-rotation", ref transform.EulerAngles, "%.3f", ImGuiInputTextFlags.None);
        //var rotationField = fieldStatus.NextField();

        if (fieldStatus.HasEdited(out _))
        {
            prop.SetValue(value);
            //if (rotationField != -1)
               // transform.ApplyRotationFromEuler();

        }
    }


    private static void DrawRenderProperty(ProxyPropertyEntry<SourceProperty> prop, ref ZaUtf8SpanWriter za)
    {
        var value=prop.GetValue();
        ImGui.TextUnformatted("Model:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AppendEnd(value.Model).AsSpan());
        za.Clear();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted(za.AppendEnd(value.MaterialKey).AsSpan());
        za.Clear();
    }

    private static void DrawParticleProperty(ProxyPropertyEntry<ParticleProperty> prop, ref ZaUtf8SpanWriter za)
    {
        var value=prop.GetValue();
        ref var def = ref value.Definition;
        ref var state = ref value.EmitterState;

        var fieldStatus = new ImGuiFieldStatus();

        ImGui.SeparatorText("Particle Component"u8);

        ImGui.TextUnformatted("ID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(value.EmitterHandle).AppendEndOfBuffer().AsSpan());

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

    private static void DrawAnimationProperty(ProxyPropertyEntry<AnimationProperty> prop, ref ZaUtf8SpanWriter za)
    {
        var value=prop.GetValue();

        var fieldStatus = new ImGuiFieldStatus();
        ImGui.SeparatorText("Animation Component"u8);

        ImGui.TextUnformatted("ID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(value.Animation).AppendEndOfBuffer().AsSpan());

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted("Clip - Length: "u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(value.ClipCount).AppendEndOfBuffer().AsSpan());
        ImGui.Separator();
        if (ImGui.InputInt("##ani-prop-clip"u8, ref value.Clip, 1))
            value.Clip = int.Clamp(value.Clip, 0, value.ClipCount - 1);

        fieldStatus.NextField();

        ImGui.TextUnformatted("Speed"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ani-speed"u8, ref value.Speed);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Duration"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-dura"u8, ref value.Duration);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Time"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-time"u8, ref value.Time);
        fieldStatus.NextField();

        if (fieldStatus.HasEdited(out _))
        {
        }
    }
}