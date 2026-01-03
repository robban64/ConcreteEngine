using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components;

internal static class EntitiesComponent
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    private static ModelStateContext Context => ModelManager.EntitiesStateContext;

    public static void Draw()
    {
        ImGui.SeparatorText("Entities"u8);
        DrawEntityList();
    }

    private static FrameStepper _frameStepper = new(8);

    private static void DrawEntityList()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
                                      ImGuiTableFlags.ScrollY;
        

        if (!ImGui.BeginTable("##entity_list_tbl"u8, 3, flags)) return;

        ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Model"u8, ImGuiTableColumnFlags.WidthFixed, ColumnWidth);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Id"u8);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Name"u8);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Model"u8);

        DrawList();

        ImGui.EndTable();
    }


    private static unsafe void DrawList()
    {
        var rowHeight = RowHeight + (ImGui.GetStyle().CellPadding.Y * 2);
        var clipper = new ImGuiListClipper();
        clipper.Begin(ManagedStore.EntitySpan.Length, rowHeight);

        Span<byte> buffer = stackalloc byte[32];
        var za = ZaUtf8SpanWriter.Create(buffer);

        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                DrawListItem(i, za);
        }

        clipper.End();
    }

    private static void DrawListItem(int i, ZaUtf8SpanWriter za)
    {
        var entity = ManagedStore.EntitySpan[i];
        var selected = entity.Id == EditorDataStore.SelectedEntity;
        //if (selected) _selectedIndex = i;

        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(za.Append(entity.Id).AppendEndOfBuffer().AsSpan());
        ImGui.TableNextRow();
        
        ImGui.TableNextColumn();
        if (EntitySelectable(za.AsSpan(), selected))
            Context.TriggerEvent(EventKey.SelectionChanged, entity);

        
        za.Clear();
        ImGui.TableNextColumn();
        var name = entity.Name.Length > 0 ? entity.Name : entity.DisplayName;
        GuiUtils.CenterAlignText(za.AppendEnd(name).AsSpan(), RowHeight);
        za.Clear();

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(za.Append(entity.Model).AppendEndOfBuffer().AsSpan(), RowHeight);
        za.Clear();

        ImGui.PopID();
        ImGui.PopStyleVar();
    }

    public static void DrawProperties()
    {
        if (!EditorDataStore.SelectedEntity.IsValid) return;

        float childHeight = ImGui.GetContentRegionAvail().Y - 2;
        if (ImGui.BeginChild("##right-sidebar-properties"u8, new Vector2(0, childHeight),
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY,
                ImGuiWindowFlags.AlwaysVerticalScrollbar |
                ImGuiWindowFlags.NoBringToFrontOnFocus))
        {
            DrawCoreProperties();
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

            ImGui.EndChild();
        }
    }

    private static void DrawCoreProperties()
    {
        ref var state = ref EditorDataStore.EntityState;
        ref var transform = ref state.Transform;
        var fieldStatus = new ImGuiFieldStatus();

        ImGui.SeparatorText("Entity Component"u8);
        ImGui.TextUnformatted("Model:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted("0"u8);

        ImGui.TextUnformatted("Material:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted("0"u8);

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
        var rotationField = fieldStatus.NextField();

        if (fieldStatus.HasEdited(out _))
        {
            if (rotationField != -1)
                EditorDataStore.EntityState.Transform.ApplyRotationFromEuler();

            EngineController.CommitEntity();
        }
    }

    private static void DrawAnimationProperties()
    {
        ref var state = ref EditorDataStore.AnimationState;
        var fieldStatus = new ImGuiFieldStatus();

        Span<byte> buffer = stackalloc byte[32];
        var za = ZaUtf8SpanWriter.Create(buffer);

        ImGui.SeparatorText("Animation Component"u8);

        ImGui.TextUnformatted("ID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(state.Animation).AppendEndOfBuffer().AsSpan());

        ImGui.TextUnformatted("Model:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(state.Model).AppendEndOfBuffer().AsSpan());

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted("Clip - Length: "u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(state.ClipCount).AppendEndOfBuffer().AsSpan());
        ImGui.Separator();
        if (ImGui.InputInt("##ani-prop-clip"u8, ref state.Clip, 1))
            state.Clip = int.Clamp(state.Clip, 0, state.ClipCount - 1);

        fieldStatus.NextField();

        ImGui.TextUnformatted("Speed"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ani-speed"u8, ref state.Speed);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Duration"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-dura"u8, ref state.Duration);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Time"u8);
        ImGui.Separator();
        ImGui.InputFloat("##ent-time"u8, ref state.Time);
        fieldStatus.NextField();

        if (fieldStatus.HasEdited(out _))
        {
            EngineController.CommitAnimation();
        }
    }

    private static void DrawParticleProperties()
    {
        ref var particle = ref EditorDataStore.ParticleState;
        ref var def = ref particle.Definition;
        ref var state = ref particle.EmitterState;

        var fieldStatus = new ImGuiFieldStatus();

        Span<byte> buffer = stackalloc byte[32];
        var za = ZaUtf8SpanWriter.Create(buffer);
        
        ImGui.SeparatorText("Particle Component"u8);

        ImGui.TextUnformatted("ID:"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.Append(EditorDataStore.ParticleState.EmitterHandle).AppendEndOfBuffer().AsSpan());

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


        if (fieldStatus.HasEdited(out _))
        {
            EngineController.CommitParticle();
        }
    }

    private static bool EntitySelectable(ReadOnlySpan<byte> str, bool selected)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var textWidth = ImGui.CalcTextSize(str).X;
        var offset = (ColumnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        return ImGui.Selectable(str, selected, flags, new Vector2(0, RowHeight));
    }
}