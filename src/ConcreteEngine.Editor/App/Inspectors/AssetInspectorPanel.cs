using System.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.App.UI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed unsafe class AssetInspectorPanel
{
    private const string ValidNoneAlphaNumericChars = ":/_-.";

    private static SelectionManager Selection => SelectionManager.Instance;

    public static TexturePtrHandle PopupTextureHandle;

    private readonly NativeString _title;

    private readonly StateManager _state;
    private readonly TextureInspectorUi _textureProxyUi;
    private readonly MaterialInspectorUi _materialProxyUi;
    private readonly ShaderInspectorUi _shaderInspectorUi;
    private readonly ModelInspectorUi _modelInspectorUi;

    private readonly TextInput _searchInput;

    private AssetId _previousId = AssetId.Empty;

    private Popup _popup = new(new Vector2(12f, 10f));

    public AssetInspectorPanel(StateManager state)
    {
        _state = state;
        _title = StringArena.AllocateString(24);
        _searchInput = new TextInput("name", 64, OnNameInput)
            {
                Trim = true, Filter = TextInputFilter.AsciiLettersAndDigit, Whitelist = ValidNoneAlphaNumericChars
            }
            .WithMinLength(4)
            .ToggleFlag(ImGuiInputTextFlags.EnterReturnsTrue, true);

        _textureProxyUi = new TextureInspectorUi(state);
        _materialProxyUi = new MaterialInspectorUi(state);
        _shaderInspectorUi = new ShaderInspectorUi(state);
        _modelInspectorUi = new ModelInspectorUi(state);
    }

    private void OnNameInput(Span<char> text)
    {
        if (Selection.SelectedAsset is not { } inspectAsset) return;
        if (text.Equals(inspectAsset.Name, StringComparison.Ordinal)) return;
        _state.EnqueueEvent(new AssetEvent(inspectAsset.Id, inspectAsset.Kind, Rename: text.ToString()));
    }


    public void OnLeave()
    {
        _previousId = AssetId.Empty;
        _searchInput.Text.Clear();
    }

    private void OnNewInspector(AssetObject asset)
    {
        RestoreName(asset);
        _previousId = asset.Id;
        _title.OverWriter.Append(asset.Kind.ToText()).Append(" - ["u8).Append(asset.Id).Append(']').End();
    }

    private void RestoreName(AssetObject asset)
    {
        _searchInput.Text.Set(asset.Name);
    }

    public void Draw()
    {
        if (Selection.SelectedAsset is not { } asset) return;

        if (_previousId != asset.Id)
            OnNewInspector(asset);

        ImGui.PushID(asset.Id.Id);

        DrawHeader(asset);
        ImGui.Spacing();
        ImGui.Separator();

        switch (asset)
        {
            case Shader shader:
                _shaderInspectorUi.Draw(shader);
                break;
            case Model model:
                _modelInspectorUi.Draw(model);
                break;
            case Texture texture:
                _textureProxyUi.Draw(texture);
                break;
            case Material material:
                _materialProxyUi.Draw(material);
                break;
        }

        ImGui.PopID();
    }

    private void DrawHeader(AssetObject asset)
    {
        ImGui.BeginGroup();
        if (AppDraw.Button(asset.Kind.ToIcon())) _popup.State = true;

        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Text, asset.Kind.ToColor());
        ImGui.SeparatorText(_title);

        ImGui.PopStyleColor();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        if (AppDraw.Button(IconNames.Undo2))
        {
            RestoreName(asset);
        }

        ImGui.SameLine();
        _searchInput.Draw();

        ImGui.EndGroup();

        var pos = ImGui.GetItemRectMin() - new Vector2(200, 50);
        if (_popup.Begin("asset-files"u8, pos))
        {
            DrawFilesTable(asset.Id);
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

        var sw = ScratchBuffer.Writer();
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
}