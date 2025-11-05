using System.Numerics;
using Core.DebugTools.Data;
using Core.DebugTools.Definitions;
using Core.DebugTools.Editor;
using Core.DebugTools.Utils;
using ImGuiNET;

namespace Core.DebugTools.Gui;

internal sealed class EntityList
{
    private const int RowHeight = 32;
    private const int ColumnWidth = 28;

    private static readonly char[] CharBuffer = new char[8];

    private readonly EditorStateContext _ctx;

    private readonly EntityListViewModel _viewModel;
    
    private int _modelId = 0;
    private int _materialTagKey = 0;

    private Vector3 _translation = Vector3.Zero;
    private Vector3 _scale = Vector3.One;
    private Vector3 _rotation = Vector3.Zero;

    public EntityList(EditorStateContext ctx)
    {
        _ctx = ctx;
        _viewModel = ctx.EntityListViewModel;
    }

    private void UpdateStateFrom(EntityViewModel? entity)
    {
        if (entity is null)
        {
            _modelId = 0;
            _materialTagKey = 0;
            _translation = Vector3.Zero;
            _scale = Vector3.One;
            _rotation = Vector3.Zero;
            return;
        }

        var transform = entity.Transform;
        _modelId = entity.Model.ModelId;
        _materialTagKey = entity.Model.MaterialTagKey;
        _translation = transform.Position;
        _scale = transform.Scale;
        _rotation = transform.Rotation;


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
        //Span<char> buffer = stackalloc char[8];
        var formatter = new NumberSpanFormatter(CharBuffer);

        foreach (var entity in _viewModel.Entities)
        {
            var selected = entity.EntityId == _viewModel.SelectedEntityId;

            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, GuiTheme.SelectedColor);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, GuiTheme.SelectedColor);
            ImGui.PushStyleColor(ImGuiCol.Header, GuiTheme.PrimaryColor);

            ImGui.PushID(entity.EntityId);
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var bufferStr = formatter.Format(entity.EntityId);
            if (EntitySelectable(bufferStr, selected))
            {
                _viewModel.SelectedEntityId = entity.EntityId;
                UpdateStateFrom(entity);

                var itemMin = ImGui.GetItemRectMin();
                var itemMax = ImGui.GetItemRectMax();
                ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
                ImGui.OpenPopup(bufferStr);
            }

            ImGui.TableNextColumn();
            bufferStr = formatter.Format(entity.ComponentCount);
            GuiUtils.CenterAlignText(bufferStr, RowHeight);

            ImGui.TableNextColumn();
            GuiUtils.CenterAlignText(entity.TransformSummary, RowHeight);

            bufferStr = formatter.Format(entity.EntityId);
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12, 10));
            ImGui.SetNextWindowSize(new Vector2(250,0));
            if (ImGui.BeginPopup(bufferStr, ImGuiWindowFlags.NoResize))
            {
                DrawAssetFilePopupContent(entity);
                ImGui.EndPopup();
            }
            ImGui.PopStyleVar();


            ImGui.PopID();

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();
        }
    }

    
    private void DrawAssetFilePopupContent(EntityViewModel entity)
    {
        ImGui.SeparatorText("Model");
        ImGui.TextUnformatted("ModelId");
        if (ImGui.InputInt("##model-id", ref _modelId, 0, 0, ImGuiInputTextFlags.None))
        {
            
        }

        ImGui.TextUnformatted("MaterialTagKey");
        if (ImGui.InputInt("##mat-tag", ref _materialTagKey, 0, 0, ImGuiInputTextFlags.None))
        {
            
        }

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Transform");
        
        ImGui.TextUnformatted("Translation");
        ImGui.Separator();
        if (ImGui.InputFloat3("##translation", ref _translation, "%.3f", ImGuiInputTextFlags.None))
        {
            Console.WriteLine(_translation.ToString());
        }
        
        ImGui.TextUnformatted("Scale");
        ImGui.Separator();
        if (ImGui.InputFloat3("##scale", ref _scale, "%.3f", ImGuiInputTextFlags.None))
        {
            
        }

        ImGui.TextUnformatted("Rotation");
        ImGui.Separator();
        if (ImGui.InputFloat3("##rotation", ref _rotation, "%.3f", ImGuiInputTextFlags.None))
        {
            
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