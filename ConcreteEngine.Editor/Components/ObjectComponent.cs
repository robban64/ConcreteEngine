#region

using System.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Store.Resources;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Components;

/*

internal static class ObjectComponent
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 36;

    private static int _editedField = -1;

    private static ModelStateContext<ObjectComponentState> Context => ModelManager.WorldObjectStateContext;
    private static ObjectComponentState State => Context.State!;


    public static void Draw()
    {
        var slotHandle = EditorDataStore.Slot<EditorParticleState>.Data.EmitterHandle;
        if (slotHandle == 0 && State.SelectedResource != 0)
            State.OnDeselectParticle();

        _editedField = -1;

        ImGui.Dummy(new Vector2(0, 2));

        if (ImGui.BeginChild("##left-sidebar-wobject-header", new Vector2(0), ImGuiChildFlags.AutoResizeY))
        {
            DrawSelector();

            ImGui.EndChild();
        }

        ImGui.Dummy(new Vector2(0, 2));
        if (ImGui.BeginChild("##left-sidebar-wobject", new Vector2(0, 0),
                ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AlwaysUseWindowPadding))
        {
            switch (State.Selection)
            {
                case WorldObjectSelection.Particle:
                    DrawParticleList();
                    break;
                case WorldObjectSelection.Animation:
                    DrawAnimationList();
                    break;
            }

            ImGui.EndChild();
        }


        if (_editedField >= 0)
        {
            _editedField = -1;
        }
    }

    private static void DrawParticleList()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
                                      ImGuiTableFlags.ScrollY;

        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);

        ImGui.SeparatorText("Particle Emitters");
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(12, 0));
        if (!ImGui.BeginTable("##wobj-particle-table", 3, flags)) return;

        ImGui.TableSetupColumn("Id##ParticleId", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("ModelId##ParticleModel", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Name##ParticleName", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Particle");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Model");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Name");

        foreach (var particle in State.Particles)
            DrawListItem(particle, formatter);

        ImGui.PopStyleVar();
        ImGui.EndTable();

        return;

        static void DrawListItem(EditorParticleResource it, NumberSpanFormatter formatter)
        {
            var selected = State.SelectedResource == it.Id;
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

            ImGui.PushID(it.Id);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            if (GuiUtils.ColumnSelectable(formatter.Format(it.Id), selected, ColumnWidth, RowHeight))
            {
                if (selected)
                    State.OnDeselectParticle();
                else
                    State.OnSelectParticle(it);
            }

            ImGui.TableNextColumn();
            GuiUtils.CenterAlignText(formatter.Format(it.MeshId), RowHeight);

            ImGui.TableNextColumn();
            GuiUtils.CenterAlignText(it.Name, RowHeight);

            ImGui.PopID();
            ImGui.PopStyleVar();
        }
    }

    private static void DrawAnimationList()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody |
                                      ImGuiTableFlags.ScrollY;

        var formatter = new NumberSpanFormatter(StringUtils.CharBuffer8);

        ImGui.SeparatorText("Animations");
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(12, 0));
        if (!ImGui.BeginTable("##wobj-animations-table", 4, flags)) return;

        ImGui.TableSetupColumn("Id##AnimationId", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Model##AnimationModel", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Name##AnimationName", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Clips##AnimationClip", ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Id");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Model");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Name");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Clips");

        foreach (var particle in State.Animations)
            DrawListItem(particle, formatter);

        ImGui.PopStyleVar();
        ImGui.EndTable();

        return;

        static void DrawListItem(EditorAnimationResource it, NumberSpanFormatter formatter)
        {
            var selected = State.SelectedResource == it.Id;
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

            ImGui.PushID(it.Id);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            if (GuiUtils.ColumnSelectable(formatter.Format(it.Id), selected, ColumnWidth, RowHeight))
            {
                State.OnSelectAnimation(it);

                var itemMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();
            }

            ImGui.TableNextColumn();
            GuiUtils.CenterAlignText(formatter.Format(it.ModelId), RowHeight);

            ImGui.TableNextColumn();
            GuiUtils.CenterAlignText(it.Name, RowHeight);

            ImGui.TableNextColumn();
            GuiUtils.CenterAlignText(formatter.Format(it.Clips.Length), RowHeight);

            ImGui.PopID();
            ImGui.PopStyleVar();
        }
    }

    private static void DrawSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 8f);
        var title = State.Selection switch
        {
            WorldObjectSelection.Particle => "Particles",
            WorldObjectSelection.Animation => "Animations",
            _ => "Select Object"
        };
        if (ImGui.BeginCombo("##wobj-combo", title, ImGuiComboFlags.HeightLargest))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 12));

            if (ImGui.Selectable("Particle", false, 0, default))
                State.OnSelectionChange(WorldObjectSelection.Particle);
            else if (ImGui.Selectable("Animation", false, 0, default))
                State.OnSelectionChange(WorldObjectSelection.Animation);


            ImGui.PopStyleVar();
            ImGui.EndCombo();
        }

        ImGui.PopStyleVar(2);
    }
}*/