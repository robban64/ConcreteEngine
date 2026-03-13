using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Theme.Widgets;
using ConcreteEngine.Editor.UI.Inspector;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class AssetInspectorPanel(StateContext context, AssetController assetController)
    : EditorPanel(PanelId.AssetInspector, context)
{
    private static readonly char[] ValidNoneAlphaNumericChars = [':', '/', '_', '-', '.'];

    [FixedAddressValueType] private static String64Utf8 _nameBuffer;
    private static void RestoreName(InspectAsset asset) => _nameBuffer = new String64Utf8(asset.Name);

    private readonly TextureInspectorUi _textureProxyUi = new(context, assetController);
    private readonly MaterialInspectorUi _materialProxyUi = new(context, assetController);
    private readonly ShaderInspectorUi _shaderInspectorUi = new(context, assetController);
    private readonly ModelInspectorUi _modelInspectorUi = new(context, assetController);

    private Popup _popup = new(new Vector2(12f, 10f));

    private AssetId _previousId = AssetId.Empty;

    private NativeViewPtr<byte> _titleStrPtr = TextBuffers.PersistentArena.Alloc(24);

    private void OnNewInspector(InspectAsset inspector)
    {
        RestoreName(inspector);
        _previousId = inspector.Id;

        _titleStrPtr.Writer().Append(inspector.Kind.ToText()).Append(" - ["u8).Append(inspector.Id).Append(']').End();
    }

    public override void Enter()
    {
    }

    public override void Leave()
    {
        _previousId = AssetId.Empty;
        _nameBuffer.AsSpan().Clear();
    }


    public override void Draw(FrameContext ctx)
    {
        if (Context.Selection.SelectedAsset is not { } inspector) return;

        if (_previousId != inspector.Id)
            OnNewInspector(inspector);

        ImGui.PushID(inspector.Id);

        DrawHeader(inspector, ctx);
        ImGui.Spacing();
        ImGui.Separator();

        switch (inspector)
        {
            case InspectShader shader:
                _shaderInspectorUi.Draw(shader, ctx);
                break;
            case InspectModel model:
                _modelInspectorUi.Draw(model, ctx);
                break;
            case InspectTexture texture:
                _textureProxyUi.Draw(texture, ctx);
                break;
            case InspectMaterial material:
                _materialProxyUi.Draw(material, ctx);
                break;
        }

        ImGui.PopID();
    }

    private void DrawHeader(InspectAsset inspectAsset, FrameContext ctx)
    {
        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(inspectAsset.GetIcon()))) _popup.State = true;

        ImGui.SameLine();

        ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetAssetColor(inspectAsset.Kind));
        ImGui.SeparatorText(_titleStrPtr);

        ImGui.PopStyleColor();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(Icons.Undo2)))
        {
            RestoreName(inspectAsset);
        }

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, ref _nameBuffer.GetRef(), 64, GuiTheme.InputNameFlags, InputCallback))
        {
            HandleRename(inspectAsset);
        }

        ImGui.EndGroup();

        var pos = new Vector2(ImGui.GetItemRectMin().X - 200, ImGui.GetItemRectMin().Y - 50);
        if (_popup.Begin("asset-files"u8, pos))
        {
            DrawFilesTable(inspectAsset.FileSpecs, ctx);
            _popup.End();
        }
    }

    private static void DrawFilesTable(AssetFileSpec[] fileSpecs, FrameContext ctx)
    {
        ImGui.SeparatorText("Files"u8);
        if (!ImGui.BeginTable("##asset_store_files_tbl"u8, 4, ImGuiTableFlags.Borders)) return;

        var layout = new TableLayout()
            .Row("ID"u8).RowStretch("Path"u8).Row("Size"u8).Row("Hash"u8);

        ImGui.TableHeadersRow();
        foreach (var it in fileSpecs)
        {
            ImGui.PushID(it.Id.Value);
            ImGui.TableNextRow();
            layout.Column(ctx.Sw.Write(it.Id.Value));
            layout.Column(ctx.Sw.Write(it.RelativePath));
            layout.Column(ctx.Sw.Write(it.SizeBytes));
            layout.Column(ctx.Sw.Write(it.ContentHash ?? ""));
            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private static void HandleRename(InspectAsset inspectAsset)
    {
        UtfText.SliceNullTerminate(_nameBuffer.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;
        if (!UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        chars = chars.Trim();
        if (chars.IsEmpty || chars.Equals(inspectAsset.Asset.Name, StringComparison.Ordinal)) return;

        var name = chars.ToString();
        inspectAsset.Rename(name);
        // Context.EnqueueEvent(new AssetUpdateEvent(AssetUpdateEvent.EventAction.Rename, inspectAsset.Id, name));
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