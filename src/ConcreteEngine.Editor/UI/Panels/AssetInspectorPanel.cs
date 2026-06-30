using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class AssetInspectorPanel : EditorPanel
{
    private static readonly char[] ValidNoneAlphaNumericChars = [':', '/', '_', '-', '.'];

    private static SelectionManager Selection => SelectionManager.Instance;

    public static TexturePtrHandle PopupTextureHandle;

    private AssetId _previousId = AssetId.Empty;

    private readonly TextureInspectorUi _textureProxyUi;
    private readonly MaterialInspectorUi _materialProxyUi;
    private readonly ShaderInspectorUi _shaderInspectorUi;
    private readonly ModelInspectorUi _modelInspectorUi;

    private readonly TextInput _searchInput;

    private NativeString _title;
    private NativeString _nameInputStr;
    
    private Popup _popup = new(new Vector2(12f, 10f));

    public AssetInspectorPanel(StateManager state) : base(InspectorId.Asset, state)
    {
        _textureProxyUi = new TextureInspectorUi(state);
        _materialProxyUi = new MaterialInspectorUi(state);
        _shaderInspectorUi = new ShaderInspectorUi(state);
        _modelInspectorUi = new ModelInspectorUi(state);

        _searchInput = new TextInput("name", 64,
                ImGuiInputTextFlags.CharsNoBlank | ImGuiInputTextFlags.EnterReturnsTrue)
            .WithFilter(TextInputFilter.AsciiLettersAndDigit, whiteListFilter: ValidNoneAlphaNumericChars)
            .WithMinLength(4)
            .WithTransformer(trimmed: true)
            .WithCallbackU16((value) =>
            {
                if (Selection.SelectedAsset is not { } inspectAsset) return;
                if (value.Equals(inspectAsset.Name, StringComparison.Ordinal)) return;
                State.EnqueueEvent(new AssetEvent(inspectAsset.Id, inspectAsset.Kind, Rename: value.ToString()));
            });
    }

    public override void OnCreate()
    {
        _title = StringArena.AllocateString(24);
        _nameInputStr = StringArena.AllocateString(64);
        _searchInput.SetTextBuffer(_nameInputStr);
    }

    public override void OnAttach()
    {
        _searchInput.SetTextBuffer(_nameInputStr);
    }

    public override void OnLeave()
    {
        _previousId = AssetId.Empty;
        _searchInput.UnsetTextBuffer();
    }

    private void OnNewInspector(InspectAsset inspector)
    {
        RestoreName(inspector);
        _previousId = inspector.Id;

        _title.NewWrite.Append(inspector.Kind.ToText()).Append(" - ["u8).Append(inspector.Id.Id).Append(']').End();
    }

    private void RestoreName(InspectAsset inspector)
    {
        _nameInputStr.Set(inspector.Name);
    }

    public override void OnDraw()
    {
        if (Selection.SelectedAsset is not { } inspector) return;

        if (_previousId != inspector.Id)
            OnNewInspector(inspector);

        ImGui.PushID(inspector.Id.Id);

        DrawHeader(inspector);
        ImGui.Spacing();
        ImGui.Separator();

        switch (inspector)
        {
            case InspectShader shader:
                _shaderInspectorUi.Draw(shader);
                break;
            case InspectModel model:
                _modelInspectorUi.Draw(model);
                break;
            case InspectTexture texture:
                _textureProxyUi.Draw(texture);
                break;
            case InspectMaterial material:
                _materialProxyUi.Draw(material);
                break;
        }

        ImGui.PopID();
    }

    private void DrawHeader(InspectAsset inspectAsset)
    {
        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(inspectAsset.GetIcon()))) _popup.State = true;

        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetAssetColor(inspectAsset.Kind));
        ImGui.SeparatorText(_title);

        ImGui.PopStyleColor();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(Icons.Undo2)))
        {
            RestoreName(inspectAsset);
        }

        ImGui.SameLine();
        _searchInput.Draw();

        ImGui.EndGroup();

        var pos = ImGui.GetItemRectMin() - new Vector2(200, 50);
        if (_popup.Begin("asset-files"u8, pos))
        {
            DrawFilesTable(inspectAsset.Id);
            _popup.End();
        }
    }

    private static void DrawFilesTable(AssetId assetId)
    {
        ImGui.SeparatorText("Files"u8);
        if (!ImGui.BeginTable("##asset_store_files_tbl"u8, 5, ImGuiTableFlags.Borders)) return;

        ImGui.TableSetupColumn("ID"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Path"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("LastWritten"u8, ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableHeadersRow();

        var sw = TextBuffers.GetWriter();
        foreach (var it in AssetManager.GetAssetBindingsEnumerator(assetId))
        {
            ImGui.PushID(it.Id);
            ImGui.TableNextRow();
            AppDraw.TextColumn(sw.Write(it.Id));
            AppDraw.TextColumn(sw.Write(it.LogicalName));
            AppDraw.TextColumn(sw.Write(it.RelativePath));
            AppDraw.TextColumn(sw.Write(it.SizeBytes));
            AppDraw.TextColumn(sw.Write(it.LastWriteTime, "yy-MM-dd HH:mm:ss"));
            ImGui.PopID();
        }

        ImGui.EndTable();
    }


    private static int InputCallback(ImGuiInputTextCallbackData* data)
    {
        if (data->EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
        {
            var c = (char)data->EventChar;
            if (char.IsAsciiDigit(c) || char.IsAsciiLetterOrDigit(c)) return 0;
            if (ValidNoneAlphaNumericChars.AsSpan().Contains(c)) return 0;
            return 1;
        }

        return 0;
    }
}