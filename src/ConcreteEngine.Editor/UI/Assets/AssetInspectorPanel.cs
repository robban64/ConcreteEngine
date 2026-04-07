using System.Numerics;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Extensions;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Theme.Widgets;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class AssetInspectorPanel(StateContext context)
    : EditorPanel(PanelId.AssetInspector, context)
{
    private static readonly char[] ValidNoneAlphaNumericChars = [':', '/', '_', '-', '.'];

    private AssetId _previousId = AssetId.Empty;

    private NativeView<byte> _inputStrPtr;
    private NativeView<byte> _titleStrPtr;

    private readonly TextureInspectorUi _textureProxyUi = new(context);
    private readonly MaterialInspectorUi _materialProxyUi = new(context);
    private readonly ShaderInspectorUi _shaderInspectorUi = new(context);
    private readonly ModelInspectorUi _modelInspectorUi = new(context);

    private Popup _popup = new(new Vector2(12f, 10f));

    public override void OnCreate()
    {
        var builder = CreateAllocBuilder();
        _inputStrPtr = builder.AllocSlice(64);
        _titleStrPtr = builder.AllocSlice(24);
        PanelMemory = builder.Commit();
    }

    public override void OnLeave()
    {
        _titleStrPtr.Clear();
        _previousId = AssetId.Empty;
    }

    private void OnNewInspector(InspectAsset inspector)
    {
        RestoreName(inspector);
        _previousId = inspector.Id;

        _titleStrPtr.Writer().Append(inspector.Kind.ToText()).Append(" - ["u8).Append(inspector.Id).Append(']')
            .EndPtr();
    }

    private void RestoreName(InspectAsset inspector)
    {
        _inputStrPtr.Clear();
        _inputStrPtr.Writer().Write(inspector.Name);
    }

    public override void OnDraw(FrameContext ctx)
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
        if (ImGui.InputText("##name"u8, _inputStrPtr, 64, GuiTheme.InputNameFlags, InputCallback))
        {
            HandleRename(inspectAsset);
        }

        ImGui.EndGroup();

        var pos = ImGui.GetItemRectMin() - new Vector2(200, 50);
        if (_popup.Begin("asset-files"u8, pos))
        {
            DrawFilesTable(inspectAsset.Id, ctx.Sw);
            _popup.End();
        }
    }

    private static void DrawFilesTable(AssetId assetId, UnsafeSpanWriter sw)
    {
        ImGui.SeparatorText("Files"u8);
        if (!ImGui.BeginTable("##asset_store_files_tbl"u8, 4, ImGuiTableFlags.Borders)) return;

        ImGui.TableSetupColumn("ID"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Path"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Hash"u8, ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableHeadersRow();

        var assetProvider = EngineObjectStore.AssetProvider;
        foreach (var it in assetProvider.AssetBindingsEnumerator(assetId))
        {
            ImGui.PushID(it.Id.Value);
            ImGui.TableNextRow();
            AppDraw.Column(sw.Write(it.Id.Value));
            AppDraw.Column(sw.Write(it.RelativePath));
            AppDraw.Column(sw.Write(it.SizeBytes));
            AppDraw.Column(sw.Write(it.ContentHash ?? ""));
            ImGui.PopID();
        }

        ImGui.EndTable();
    }


    private void HandleRename(InspectAsset inspector)
    {
        var byteSpan = _inputStrPtr.AsSpan().SliceNullTerminate();
        if (byteSpan.IsEmpty || !UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        chars = chars.Trim();
        if (chars.IsEmpty || chars.Equals(inspector.Asset.Name, StringComparison.Ordinal)) return;

        var name = chars.ToString();
        Context.EnqueueEvent(new AssetEvent(EventAction.Rename, inspector.Id, name));
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