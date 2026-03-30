using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.EditorConsts;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class SceneListPanel : EditorPanel
{
    private const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.ScrollY |
        ImGuiTableFlags.NoPadOuterX |
        ImGuiTableFlags.NoPadInnerX |
        ImGuiTableFlags.SizingFixedFit;


    private const float ListItemHeight = 20f;
    private const float ListItemPad = 4f;

    private static readonly Vector2 VisBtnSize = new(ListItemHeight, ListItemHeight);

    private readonly SceneObjectId[] _sceneIds = new SceneObjectId[SceneCapacity];
    private SceneObjectKind _selectedKind;
    private int _sceneCount;

    private readonly SceneController _controller = EngineObjectStore.SceneController;
    private  ComboField _kindCombo = null!;

    private NativeViewPtr<byte> _inputStrPtr;
    private NativeViewPtr<byte> _titleStrPtr;



    public SceneListPanel(StateContext context) : base(PanelId.SceneList, context)
    {
    }

    public override void OnCreate()
    {
        var block = AllocatePanelMemory(32);
        _inputStrPtr = block->AllocSlice(8);
        _titleStrPtr = block->AllocSlice(24);

        _kindCombo = ComboField
            .MakeFromEnumCache<SceneObjectKind>("##scene-combo", () => (int)_selectedKind, OnCategoryChange)
            .WithProperties(FieldGetDelay.VeryHigh, FieldLayout.None)
            .WithStartAt(0);
        _kindCombo.SetItemName(0, "All");

    }

    public override void OnEnter()
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


    public override void OnDraw(FrameContext ctx)
    {
        const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.CharsNoBlank;
        // search
        var width = ImGui.GetContentRegionAvail().X - GuiTheme.WindowPadding.X;
        ImGui.SetNextItemWidth(width * 0.65f);

        if (ImGui.InputText("##search-scene"u8, _inputStrPtr, 8, inputFlags))
            Search();

        ImGui.SameLine();

        ImGui.SetNextItemWidth(width * 0.35f);
        _kindCombo.Draw();

        ImGui.SeparatorText(_titleStrPtr);

        // list table
        if (ImGui.BeginTable("scene-list"u8, 2, TableFlags))
        {
            ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Visible"u8, ImGuiTableColumnFlags.WidthFixed, 28);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4f));
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);

            DrawList(ctx.Sw);

            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            ImGui.EndTable();
        }
    }

    private void DrawList(UnsafeSpanWriter sw)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(_sceneCount, ListItemHeight + ListItemPad);
        var selectedId = Context.SelectedSceneId;
        while (clipper.Step())
        {
            var idSpan = _sceneIds.AsSpan(clipper.DisplayStart, clipper.DisplayEnd - clipper.DisplayStart);
            foreach (var id in idSpan)
            {
                ImGui.PushID(id);
                var sceneObject = _controller.GetSceneObject(id);
                DrawListItem(sceneObject, id == selectedId, sw);
                ImGui.PopID();
            }
        }

        clipper.End();
    }

    private void DrawListItem(SceneObject it, bool selected, UnsafeSpanWriter sw)
    {
        const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.AllowDoubleClick;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        var cellTop = ImGui.GetCursorPosY();

        if (ImGui.Selectable("##select"u8, selected, selectFlags, new Vector2(0, ListItemHeight)))
            Context.EnqueueEvent(new SelectionEvent(it.Id));

        GuiLayout.NextAlignTextVerticalTop(cellTop, ListItemHeight);
        ImGui.TextUnformatted(sw.Append(ref *StyleMap.GetIcon(it.Kind.ToIcon())).PadRight(4).Append(it.Name).End());

        ImGui.TableNextColumn();
        if (ImGui.Button(StyleMap.GetIcon(Icons.Eye), VisBtnSize)) ;
    }

    private void Search()
    {
        _sceneIds.AsSpan(0, _sceneCount).Clear();

        Span<char> chars = stackalloc char[_inputStrPtr.Length];
        chars = InputTextUtils.GetSearchString(_inputStrPtr.AsSpan(), chars, out var searchKey, out var searchMask);
        if (!int.TryParse(chars, out var searchId)) searchId = 0;

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

        _titleStrPtr.Writer().Append("SceneObjects ["u8).Append(_sceneCount).Append(']').End();
    }
}

/*
    private const ImGuiTreeNodeFlags TreeFlags =
       ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.SpanAvailWidth |
       ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DrawLinesNone;

   private void DrawRow(SceneObject sceneObj, bool selected, FrameContext ctx)
   {
       var flags = TreeFlags;
       if (selected) flags |= ImGuiTreeNodeFlags.Selected;

       ImGui.TableNextRow(ListItemHeight);
       ImGui.TableNextColumn();
       if (ImGui.TreeNodeEx(ctx.Sw.Write(sceneObj.Name), flags))
       {
           ImGui.Text(sceneObj.GetInstances()[0].DisplayName);
           ImGui.TreePop();
       }

       if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
       {
           if (!ImGui.IsItemToggledOpen())
               Context.EnqueueEvent(new SceneObjectEvent(sceneObj.Id));
       }

       ImGui.TableNextColumn();

       ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
       if (ImGui.Button(StyleMap.GetIcon(Icons.Eye), VisBtnSize)) ;
       ImGui.PopStyleColor();
   }

 */