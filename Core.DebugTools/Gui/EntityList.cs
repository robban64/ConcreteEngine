using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Utils;
using ImGuiNET;

namespace Core.DebugTools.Gui;

internal sealed class EntityList
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 28;

    private readonly EditorStateContext _ctx;

    private readonly EntityListViewModel _viewModel;

    public EntityList(EditorStateContext ctx)
    {
        _ctx = ctx;
        _viewModel = ctx.EntityListViewModel;
    }

    public void Draw()
    {
        ImGui.SeparatorText("Entities");

        if (ImGui.BeginChild("#EntityList"))
        {
            DrawEntityList();
            ImGui.EndChild();
        }
    }

    private void DrawEntityList()
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.NoBordersInBody;

        if (!ImGui.BeginTable("##entity_list_tbl", 3, flags)) return;

        ImGui.TableSetupColumn("Id", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Comp", ImGuiTableColumnFlags.WidthFixed, ColumnWidth);
        ImGui.TableSetupColumn("Transform", ImGuiTableColumnFlags.WidthStretch);
        
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Id");

        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Comp");
        
        ImGui.TableNextColumn();
        GuiUtils.CenterAlignTextHorizontal("Transform");
        

        DrawList();

        ImGui.EndTable();
    }

    private void DrawList()
    {
        Span<char> buffer = stackalloc char[8];
        var formatter = new NumberSpanFormatter(buffer);

        foreach (var entity in _viewModel.Entities)
        {
            var selected = entity.EntityId == _viewModel.SelectedEntityId;

            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GuiTheme.HoverColor);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, GuiTheme.HoverColor);
            ImGui.PushStyleColor(ImGuiCol.Header, GuiTheme.PrimaryColor);

            ImGui.PushID(entity.EntityId);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var bufferStr = formatter.Format(entity.EntityId);
            if (EntitySelectable(bufferStr, selected))
            {
                _viewModel.SelectedEntityId = entity.EntityId;
            }

            ImGui.TableNextColumn();
            bufferStr = formatter.Format(entity.ComponentCount);
            GuiUtils.CenterAlignText(bufferStr, RowHeight);

            ImGui.TableNextColumn();
            GuiUtils.CenterAlignText(entity.TransformSummary, RowHeight);

            ImGui.PopID();

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();
        }
    }

    private static bool EntitySelectable(ReadOnlySpan<char> str, bool selected)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;
        
        var textWidth = ImGui.CalcTextSize(str).X;
        var offset = (ColumnWidth - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        return ImGui.Selectable(str, selected,flags, new Vector2(0, RowHeight));
    }
}