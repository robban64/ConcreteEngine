using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.App.UI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Inputs;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Assets;

internal sealed unsafe class AssetInspectorPanel : Inspector<AssetObject>
{
    private const string ValidNoneAlphaNumericChars = ":/_-.";

    private static SelectionManager Selection => SelectionManager.Instance;

    private readonly NativeString _title;
    private readonly StateManager _state;
    private readonly TextInput _searchInput;
    private Popup _popup;

    public override InspectorId Id => InspectorId.Asset;
    public override uint Icon { get; }

    private Inspector? _inspector;

    public AssetInspectorPanel(StateManager state)
    {
        //Sections = _fields.CreateSections();
        _ = new TextureInspector();
        _ = new MaterialInspector();
        _ = new ModelInspectorUi();
        _ = new ShaderInspectorUi { State = state };
        _state = state;
        _title = StringArena.AllocateString(24);
        _searchInput = new TextInput("name", 64, OnNameInput)
            {
                Trim = true, Filter = TextInputFilter.AsciiLettersAndDigit, Whitelist = ValidNoneAlphaNumericChars
            }
            .WithMinLength(4)
            .ToggleFlag(ImGuiInputTextFlags.EnterReturnsTrue, true);
    }

    protected override void OnAttachTarget(AssetObject? oldTarget, AssetObject newTarget)
    {
        _inspector?.DetachTarget();
        _inspector = null;

        switch (newTarget)
        {
            case Shader shader:
                _inspector = ShaderInspectorUi.Instance;
                ShaderInspectorUi.Instance.AttachTarget(shader);
                break;
            case Model model:
                _inspector = ModelInspectorUi.Instance;
                ModelInspectorUi.Instance.AttachTarget(model);
                break;
            case Texture texture:
                _inspector = TextureInspector.Instance;
                TextureInspector.Instance.AttachTarget(texture);
                break;
            case Material material:
                _inspector = MaterialInspector.Instance;
                MaterialInspector.Instance.AttachTarget(material);
                break;
        }

        RestoreName(newTarget);
        using var builder = new NativeStringBuilder(_title);
        builder.Writer.Append(newTarget.Kind.ToUtf8()).Append(" - ["u8).Append(newTarget.Id).Append(']');
    }

    private void OnNameInput(Span<char> text)
    {
        if (text.Equals(Target!.Name, StringComparison.Ordinal)) return;
        _state.EnqueueEvent(new AssetEvent(Target.Id, Target.Kind, Rename: text.ToString()));
    }

    private void RestoreName(AssetObject asset) => _searchInput.Text.Set(asset.Name);


    public override void Draw()
    {
        if (Selection.SelectedAsset is not { } asset) return;

        ImGui.PushID(asset.Id);

        DrawHeader(asset);
        ImGui.Spacing();
        ImGui.Separator();

        _inspector?.Draw();

        ImGui.PopID();
    }

    private void DrawHeader(AssetObject asset)
    {
        ImGui.BeginGroup();
        if (AppDraw.Button(Target!.Kind.ToIcon())) _popup.State = true;

        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Text, asset.Kind.ToColor());
        ImGui.SeparatorText(_title);

        ImGui.PopStyleColor();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        if (AppDraw.Button(IconNames.Undo2)) RestoreName(asset);

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
        if (!ImGui.BeginTable("##asset_files_tbl"u8, 5, ImGuiTableFlags.Borders)) return;

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