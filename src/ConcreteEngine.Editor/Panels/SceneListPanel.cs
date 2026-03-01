using System.Numerics;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Theme.Widgets;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.Panels;

internal sealed unsafe class SceneListPanel : EditorPanel
{
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
            .WithStartAt(0);
        _kindCombo.SetItemName(0, "All");
        _kindCombo.Layout = FieldLabelLayout.None;
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

        _kindCombo.Draw(width * 0.35f);

        ImGui.SeparatorText(ref WriteFormat.WriteTitleId(ctx.Sw, "SceneObjects"u8, _sceneCount));

        // list table
        GuiTheme.PushFontTextLarge();
        if (ImGui.BeginTable("scene-list"u8, 3, GuiTheme.TableFlags))
        {
            ImGui.TableSetupColumn("Icon"u8, ImGuiTableColumnFlags.WidthFixed, 36);
            ImGui.TableSetupColumn("Id"u8, ImGuiTableColumnFlags.WidthFixed, 36);
            ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);

            DrawList(ctx);

            ImGui.EndTable();
        }

        ImGui.PopFont();
    }

    private void DrawList(FrameContext ctx)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(_sceneCount, ListItemHeight + 4);
        var selectedId = Context.SelectedAssetId;
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - start;
            var idSpan = _sceneIds.AsSpan(start, length);
            foreach (var id in idSpan)
            {
                ImGui.PushID(id);
                var selected = id == selectedId;
                var sceneObject = _controller.GetSceneObject(id);
                DrawListItem(sceneObject, selected, ctx);
                ImGui.PopID();
            }
        }

        clipper.End();
    }

    private void DrawListItem(SceneObject it, bool selected, FrameContext ctx)
    {
        const ImGuiSelectableFlags
            selectFlags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;

        ImGui.PushID(it.Id);
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));
        if (ImGui.Selectable(ctx.Sw.Write(it.Id), selected, selectFlags, new Vector2(0, ListItemHeight)))
            Context.EnqueueEvent(new SceneObjectEvent(it.Id));
        ImGui.PopStyleVar();

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVertical(ListItemHeight, 15.0f);
        ImGui.TextUnformatted(ctx.Sw.Write(it.Id));

        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVertical(ListItemHeight, 15.0f);
        ImGui.TextUnformatted(ctx.Sw.Write(it.Name));

        ImGui.PopID();
    }


    private void DrawListItemOld(SceneObjectId id, bool selected, FrameContext ctx)
    {
        ImGui.PushID(id);
        ImGui.TableNextRow();

        var sceneObject = _controller.GetSceneObject(id);

        TableLayout.Make(GuiTheme.ListRowHeight, TextAlignMode.VerticalCenter)
            .ColumnColor(in StyleMap.GetSceneColor(sceneObject.Kind), ctx.Sw.Write(sceneObject.Kind.ToText()))
            .SelectableColumn(ctx.Sw.Write(id), selected, GuiTheme.IdColWidth, out var clicked)
            .Column(ctx.Sw.Write(sceneObject.Name));

        if (clicked)
            Context.EnqueueEvent(new SceneObjectEvent(id));

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