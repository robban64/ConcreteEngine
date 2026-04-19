using System.Numerics;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Extensions;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Assets;

internal sealed unsafe class AssetInspectorPanel : EditorPanel
{
    private static readonly char[] ValidNoneAlphaNumericChars = [':', '/', '_', '-', '.'];

    private AssetId _previousId = AssetId.Empty;

    private readonly TextureInspectorUi _textureProxyUi;
    private readonly MaterialInspectorUi _materialProxyUi;
    private readonly ShaderInspectorUi _shaderInspectorUi;
    private readonly ModelInspectorUi _modelInspectorUi;

    private readonly TextInput _searchInput;

    private Range32 _titleStrHandle;
    private Range32 _inputStrHandle;
    private Popup _popup = new(new Vector2(12f, 10f));

    public AssetInspectorPanel(StateContext context) : base(PanelId.AssetInspector, context)
    {
        _textureProxyUi = new TextureInspectorUi(context);
        _materialProxyUi = new MaterialInspectorUi(context);
        _shaderInspectorUi = new ShaderInspectorUi(context);
        _modelInspectorUi = new ModelInspectorUi(context);

        _searchInput = new TextInput(64, ImGuiInputTextFlags.CharsNoBlank | ImGuiInputTextFlags.EnterReturnsTrue)
            .WithFilter(TextInputFilter.AsciiLettersAndDigit, ValidNoneAlphaNumericChars)
            .WithMinLength(4)
            .WithTransformer(trimmed: true)
            .WithCallbackU16((value) =>
            {
                if (Context.SelectedAsset is not { } inspectAsset) return;
                if (value.Equals(inspectAsset.Name, StringComparison.Ordinal)) return;
                Context.EnqueueEvent(new AssetEvent(EventAction.Rename, inspectAsset.Id, value.ToString()));
            });
    }

    private NativeView<byte> TitleStr => DataPtr.Slice(_titleStrHandle);
    private NativeView<byte> InputStr => DataPtr.Slice(_inputStrHandle);


    public override void OnCreate()
    {
        var builder = CreateAllocBuilder();
        _inputStrHandle = builder.AllocSlice(64).AsRange32();
        _titleStrHandle = builder.AllocSlice(24).AsRange32();
        PanelMemory = builder.Commit();
    }

    public override void OnLeave()
    {
        TitleStr.Clear();
        _previousId = AssetId.Empty;
    }

    private void OnNewInspector(InspectAsset inspector)
    {
        RestoreName(inspector);
        _previousId = inspector.Id;

        TitleStr.Writer().Append(inspector.Kind.ToText()).Append(" - ["u8).Append(inspector.Id).Append(']').End();
    }

    private void RestoreName(InspectAsset inspector)
    {
        InputStr.Clear();
        InputStr.Writer().Write(inspector.Name);
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
        ImGui.SeparatorText(TitleStr);

        ImGui.PopStyleColor();
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(Icons.Undo2)))
        {
            RestoreName(inspectAsset);
        }

        ImGui.SameLine();
        _searchInput.Draw("##name"u8, InputStr);

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
            AppDraw.TextColumn(sw.Write(it.Id.Value));
            AppDraw.TextColumn(sw.Write(it.RelativePath));
            AppDraw.TextColumn(sw.Write(it.SizeBytes));
            AppDraw.TextColumn(sw.Write(it.ContentHash ?? ""));
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