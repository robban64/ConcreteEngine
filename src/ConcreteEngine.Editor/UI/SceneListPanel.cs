using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class SceneListPanel : EditorPanel
{
    private const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.ScrollY |
        ImGuiTableFlags.RowBg |
        ImGuiTableFlags.NoPadOuterX |
        ImGuiTableFlags.NoPadInnerX |
        ImGuiTableFlags.SizingFixedFit;
    
    private const ImGuiTreeNodeFlags TreeFlags =
        ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.SpanAvailWidth |
        ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DrawLinesNone;


    [FixedAddressValueType]
    private static SearchStringUtf8 _inputUtf8;

    private readonly SceneController _controller;

    private readonly ComboField _kindCombo;

    private SceneObjectKind _selectedKind;
    private int _sceneCount;
    private readonly SceneObjectId[] _sceneIds = new SceneObjectId[SceneCapacity];


    private const float ListItemHeight = 18f;

    public SceneListPanel(StateContext context, SceneController controller) : base(PanelId.SceneList, context)
    {
        _controller = controller;

        _kindCombo = ComboField
            .MakeFromEnumCache<SceneObjectKind>("##scene-combo", () => (int)_selectedKind, OnCategoryChange)
            .WithProperties(FieldGetDelay.VeryHigh, FieldLayout.None)
            .WithStartAt(0);
        _kindCombo.SetItemName(0, "All");
    }

    public override void Enter()
    {
        if (_sceneCount == 0) Search();
    }

    private void OnCategoryChange(Int1Value kind)
    {
        var newKind = (SceneObjectKind)kind.X;
        if (_selectedKind == newKind) return;
        _selectedKind = newKind;
        Search();
    }

    public override void Draw(FrameContext ctx)
    {
        const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.CharsNoBlank;
        // search
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.65f);

        if (ImGui.InputText("##search-scene"u8, ref _inputUtf8.GetInputRef(), SearchStringUtf8.Length, inputFlags))
            Search();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);
        _kindCombo.Draw();

        ImGui.SeparatorText(ref WriteFormat.WriteTitleId(ctx.Sw, "SceneObjects"u8, _sceneCount));

        // list table
        /*
         if (ImGui.BeginTable("scene-list"u8, 4, TableFlags))
         {
             ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 28);
             ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 36);
             ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
             ImGui.TableSetupColumn("Visible"u8, ImGuiTableColumnFlags.WidthFixed, 28);

             DrawList(ctx);

             ImGui.EndTable();
         }*/
        DrawList(ctx);
    }

    private void DrawList(FrameContext ctx)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(_sceneCount, ListItemHeight + 4);
        var selectedId = Context.SelectedSceneId;
        while (clipper.Step())
        {
            var idSpan = _sceneIds.AsSpan(clipper.DisplayStart, clipper.DisplayEnd - clipper.DisplayStart);
            foreach (var id in idSpan)
            {
                ImGui.PushID(id);
                var sceneObject = _controller.GetSceneObject(id);
                DrawNew(sceneObject, id == selectedId, ctx);
                ImGui.PopID();
            }
        }

        clipper.End();
    }

    private void DrawNew(SceneObject it, bool selected, FrameContext ctx)
    {
        var flags = TreeFlags;
        if (selected) flags |= ImGuiTreeNodeFlags.Selected;

        ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetSceneColor(it.Kind));
        AppDraw.DrawIcon(ctx.Sw.Write(IconNames.Cuboid));
        ImGui.PopStyleColor();

        ImGui.SameLine(0f, 5f);
        if (ImGui.TreeNodeEx(ctx.Sw.Write(it.Name), flags))
        {
            foreach (var entity in it.GetRenderEntities())
            {
                ImGui.TextUnformatted(ctx.Sw.Write(entity));
            }
            ImGui.TreePop();
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && !ImGui.IsItemToggledOpen())
            Context.EnqueueEvent(new SceneObjectEvent(it.Id));

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 24f);

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
        GuiTheme.PushFontIconMedium();
        if (ImGui.SmallButton(ctx.Sw.Write(IconNames.Eye))) ;
        ImGui.PopFont();
        ImGui.PopStyleColor();
    }

    private void DrawListItem(SceneObject it, bool selected, FrameContext ctx)
    {
        const ImGuiSelectableFlags
            selectFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        ImGui.PushID(it.Id);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        var cellTop = ImGui.GetCursorPosY();

        if (ImGui.Selectable("##select", selected, selectFlags, new Vector2(0, ListItemHeight)))
            Context.EnqueueEvent(new SceneObjectEvent(it.Id));

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, ListItemHeight, GuiTheme.FontSizeDefault);
        ImGui.TextUnformatted(ctx.Sw.Write(IconNames.Box));

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(cellTop, ListItemHeight, GuiTheme.FontSizeDefault);
        ImGui.TextUnformatted(ctx.Sw.Write(it.Name));

        ImGui.PopID();
    }

    private void DrawListItem2(SceneObject it, bool selected, FrameContext ctx)
    {
        const ImGuiSelectableFlags selectFlags =
            ImGuiSelectableFlags.SpanAllColumns |
            ImGuiSelectableFlags.AllowDoubleClick;

        ImGui.PushID(it.Id);
        ImGui.TableNextRow(ImGuiTableRowFlags.None, ListItemHeight);

        ImGui.TableNextColumn();
        if (ImGui.Selectable("##row", selected, selectFlags, new Vector2(0, ListItemHeight)))
            Context.EnqueueEvent(new SceneObjectEvent(it.Id));

        ImGui.SameLine(0, 0);

        ImGui.TableSetColumnIndex(0);
        GuiLayout.NextAlignTextVertical(ListItemHeight, GuiTheme.FontSizeDefault);
        ImGui.TextUnformatted("🔹"u8);

        ImGui.TableSetColumnIndex(1);
        GuiLayout.NextAlignTextVertical(ListItemHeight, GuiTheme.FontSizeDefault);
        ImGui.TextUnformatted(ctx.Sw.Write(it.Id));

        ImGui.TableSetColumnIndex(2);
        GuiLayout.NextAlignTextVertical(ListItemHeight, GuiTheme.FontSizeDefault);
        ImGui.TextUnformatted(ctx.Sw.Write(it.Name));

        ImGui.TableSetColumnIndex(3);
        GuiLayout.NextAlignTextVertical(ListItemHeight, GuiTheme.FontSizeDefault);
        if (ImGui.SmallButton("X"u8))
        {
        }

        ImGui.PopID();
    }


    private void Search()
    {
        _sceneIds.AsSpan(0, _sceneCount).Clear();
        var searchString = _inputUtf8.GetSearchString(out var searchKey, out var searchMask);
        if (!int.TryParse(searchString, out var searchId)) searchId = 0;

        var count = 0;
        var span = _controller.GetSceneObjectSpan();
        foreach (var it in span)
        {
            if (count >= AssetCapacity) break;

            if (_selectedKind > SceneObjectKind.Empty && _selectedKind != it.Kind)
                continue;

            if (searchKey <= 0 || searchId == it.Id || (it.PackedName & searchMask) == searchKey)
                _sceneIds[count++] = it.Id;
        }

        _sceneCount = count;
    }
}
/*
     public override int FilterQuery(in SearchPayload<SceneObjectId> search, SearchFilter filter,
       SearchSceneObjectDel del)
   {
       var store = _sceneStore;
       var count = 0;
       for (var i = 1; i < EnumCache<SceneObjectKind>.Count; i++)
       {
           var kind = (SceneObjectKind)i;
           var filterKind = filter.AsSceneKind;
           if (filterKind != SceneObjectKind.Empty && filterKind != kind) continue;
           var span = store.GetIdsByKindSpan(kind);
           SceneObjectItem item = default;
           foreach (var id in span)
           {
               var it = store.Get(id);
               it.ToItem(out item);
               if (del(in search, filter, in item))
                   search.Destination[count++] = it.Id;

               if (count >= EditorConsts.SceneCapacity) return count;
           }
       }

       return count;
   }

*/