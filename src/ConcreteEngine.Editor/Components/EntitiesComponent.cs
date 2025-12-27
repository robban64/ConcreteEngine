using System.Numerics;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.Components;

internal static class EntitiesComponent
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    private static ModelStateContext Context => ModelManager.EntitiesStateContext;
    
    public static void Draw()
    {
        ImGui.SeparatorText("Entities");
        DrawEntityList();
    }

    private static FrameStepper _frameStepper = new(8);

    private static void DrawEntityList()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
                                      ImGuiTableFlags.ScrollY;
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(12, 0));

        if (!ImGui.BeginTable("##entity_list_tbl", 3, flags)) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Model", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Id");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Name");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Model");

        DrawList();

        ImGui.PopStyleVar();
        ImGui.EndTable();
    }


    private static unsafe void DrawList()
    {
        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);

        var rowHeight = ImGui.GetFrameHeight();
        var clipper = new ImGuiListClipper();
        ImGuiNative.ImGuiListClipper_Begin(&clipper, ManagedStore.EntitySpan.Length, rowHeight);

        while (ImGuiNative.ImGuiListClipper_Step(&clipper) != 0)
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                DrawListItem(i, formatter);
        }

        ImGuiNative.ImGuiListClipper_End(&clipper);
    }

    private static void DrawListItem(int i, NumberSpanFormatter formatter)
    {
        var entity = ManagedStore.EntitySpan[i];
        var selected = entity.Id == EditorDataStore.SelectedEntity;
        //if (selected) _selectedIndex = i;

        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        ImGui.PushID(entity.Id);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        var bufferStr = formatter.Format(entity.Id);
        if (EntitySelectable(bufferStr, selected))
            Context.TriggerEvent(EventKey.SelectionChanged, entity);

        ImGui.TableNextColumn();
        var name = entity.Name.Length > 0 ? entity.Name : entity.DisplayName;
        GuiUtils.CenterAlignText(name, RowHeight);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignText(formatter.Format(entity.Model), RowHeight);

        ImGui.PopID();
        ImGui.PopStyleVar();
    }

    public static void DrawProperties()
    {
        if (!EditorDataStore.SelectedEntity.IsValid) return;

        float childHeight = ImGui.GetContentRegionAvail().Y - 2;
        if (ImGui.BeginChild("##right-sidebar-properties", new Vector2(0, childHeight),
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AlwaysAutoResize,
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

        ImGui.SeparatorText("Entity Component");
        ImGui.TextUnformatted("Model:");
        ImGui.SameLine();
        ImGui.TextUnformatted("0");

        ImGui.TextUnformatted("Material:");
        ImGui.SameLine();
        ImGui.TextUnformatted("0");

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform");

        ImGui.TextUnformatted("Translation");
        ImGui.Separator();
        ImGui.InputFloat3("##ent-prop-translation", ref transform.Translation, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Scale");
        ImGui.Separator();
        ImGui.InputFloat3("##ent-prop-scale", ref transform.Scale, "%.3f", ImGuiInputTextFlags.None);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Rotation");
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

        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);
        ImGui.SeparatorText("Animation Component");

        ImGui.TextUnformatted("ID:");
        ImGui.SameLine();
        ImGui.TextUnformatted(formatter.Format(state.Animation));

        ImGui.TextUnformatted("Model:");
        ImGui.SameLine();
        ImGui.TextUnformatted(formatter.Format(state.Model));

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted("Clip - Length: ");
        ImGui.SameLine();
        ImGui.TextUnformatted(formatter.Format(state.ClipCount));
        ImGui.Separator();
        if (ImGui.InputInt("##ani-prop-clip", ref state.Clip, 1))
            state.Clip = int.Clamp(state.Clip, 0, state.ClipCount - 1);

        fieldStatus.NextField();

        ImGui.TextUnformatted("Speed");
        ImGui.Separator();
        ImGui.InputFloat("##ani-speed", ref state.Speed);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Duration");
        ImGui.Separator();
        ImGui.InputFloat("##ent-dura", ref state.Duration);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Time");
        ImGui.Separator();
        ImGui.InputFloat("##ent-time", ref state.Time);
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

        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);
        ImGui.SeparatorText("Particle Component");

        ImGui.TextUnformatted("ID:");
        ImGui.SameLine();
        ImGui.TextUnformatted(formatter.Format(EditorDataStore.ParticleState.EmitterHandle));

        //DEF
        ImGui.SeparatorText("Definition");

        ImGui.BeginGroup();
        ImGui.TextUnformatted("Start Color");
        ImGui.ColorEdit4("##start-color", ref def.StartColor);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("End Color");
        ImGui.ColorEdit4("##end-color", ref def.EndColor);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Size Start / End");
        ImGui.InputFloat2("##size-start-end", ref def.SizeStartEnd);
        fieldStatus.NextField();

        ImGui.Separator();

        ImGui.TextUnformatted("Gravity");
        ImGui.InputFloat3("##gravity", ref def.Gravity);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Drag");
        ImGui.InputFloat("##drag", ref def.Drag);
        fieldStatus.NextField();

        ImGui.Separator();

        ImGui.TextUnformatted("Speed Min / Max");
        ImGui.InputFloat2("##speed-min-max", ref def.SpeedMinMax);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Life Min / Max");
        ImGui.InputFloat2("##life-min-max", ref def.LifeMinMax);
        fieldStatus.NextField();
        ImGui.EndGroup();

        //STATE
        ImGui.BeginGroup();
        ImGui.TextUnformatted("Translation");
        ImGui.InputFloat3("##translation", ref state.Translation);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Start Area");
        ImGui.InputFloat3("##start-area", ref state.StartArea);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Direction");
        ImGui.InputFloat3("##direction", ref state.Direction);
        fieldStatus.NextField();

        ImGui.TextUnformatted("Spread");
        ImGui.InputFloat("##spread", ref state.Spread);
        fieldStatus.NextField();


        if (fieldStatus.HasEdited(out _))
        {
            EngineController.CommitParticle();
        }
    }

    private static bool EntitySelectable(ReadOnlySpan<char> str, bool selected)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        var textWidth = ImGui.CalcTextSize(str).X;
        var offset = (ColumnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        return ImGui.Selectable(str, selected, flags, new Vector2(0, RowHeight));
    }
}