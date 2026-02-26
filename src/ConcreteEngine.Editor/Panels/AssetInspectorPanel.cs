using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Panels.Inspector;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.UI.Widgets;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed unsafe class AssetInspectorPanel(StateContext context, AssetController assetController)
    : EditorPanel(PanelId.AssetProperty, context)
{
    private static String64Utf8 _nameInputBuffer;

    private readonly TextureInspectorUi _textureProxyUi = new(context, assetController);
    private readonly MaterialInspectorUi _materialProxyUi = new(context, assetController);
    private readonly ShaderInspectorUi _shaderInspectorUi = new(context, assetController);
    private readonly ModelInspectorUi _modelInspectorUi = new(context, assetController);

    private Popup _popup = new(new Vector2(12f, 10f));

    private AssetId _previousId = AssetId.Empty;

    private static readonly char[] ValidNoneAlphaNumericChars = [':','/','_','-','.'];

    private static int InputCallback(ImGuiInputTextCallbackData* data)
    {
        if(data->EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
        {
            var c = (char)data->EventChar;
            if(char.IsAsciiDigit(c) || char.IsAsciiLetterOrDigit(c)) return 0;
            if(ValidNoneAlphaNumericChars.AsSpan().Contains(c)) return 0;
            return 1;
        }
        return 0;
    }

    public override void Enter()
    {
    }

    public override void Leave()
    {
        _previousId = AssetId.Empty;
        _nameInputBuffer.AsSpan().Clear();
    }

    private static void RestoreName(InspectAsset asset)
    {
        _nameInputBuffer = new String64Utf8(asset.Name);
    }

    public override void Draw(FrameContext ctx)
    {
        if (Context.Selection.SelectedAsset is not { } inspectAsset) return;

        if (_previousId != inspectAsset.Id)
        {
            RestoreName(inspectAsset);
            _previousId = inspectAsset.Id;
        }

        ImGui.PushID(inspectAsset.Id);
        DrawHeader(inspectAsset, ctx);
        ImGui.Spacing();
        ImGui.Separator();

        switch (inspectAsset)
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
        const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CharsNoBlank | ImGuiInputTextFlags.CallbackCharFilter;

        ImGui.BeginGroup();
        {
            GuiTheme.PushFontIconText();
            if (ImGui.Button(ctx.Sw.Write(inspectAsset.GetIcon()))) _popup.State = true;
            ImGui.PopFont();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetAssetColor(inspectAsset.Kind));
            ImGui.SeparatorText(ref ctx.Sw.Append(inspectAsset.Kind.ToText())
                .Append(" - ["u8).Append(inspectAsset.Id).Append(':')
                .Append(inspectAsset.Asset.Generation).Append(']').End());
            
            ImGui.PopStyleColor();
        }
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        {
            GuiTheme.PushFontIconText();
            if (ImGui.Button(ctx.Sw.Write(IconNames.Undo2)))
                RestoreName(inspectAsset);
            ImGui.PopFont();

            ImGui.SameLine();
            if (ImGui.InputText("##name"u8, ref _nameInputBuffer.GetRef(), String64Utf8.Capacity, inputFlags, InputCallback))
            {
                HandleRename(inspectAsset);
            }
        }
        ImGui.EndGroup();

        var pos = new Vector2(ImGui.GetItemRectMin().X - 200, ImGui.GetItemRectMin().Y - 50);
        if (_popup.Begin("asset-file-specs"u8, pos))
        {
            DrawFilesTable(inspectAsset.FileSpecs, ctx);
            _popup.End();
        }
    }

    private void HandleRename(InspectAsset inspectAsset)
    {
        UtfText.SliceNullTerminate(_nameInputBuffer.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;
        if(!UtfText.IsAscii(byteSpan)) return;

        //var charLength = Math.Min(Encoding.UTF8.GetCharCount(byteSpan), String64Utf8.Capacity);
        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        chars = chars.Trim();
        if (chars.IsEmpty || chars.Equals(inspectAsset.Asset.Name, StringComparison.Ordinal)) return;

        var name = chars.ToString();
        inspectAsset.Rename(name);
       // Context.EnqueueEvent(new AssetUpdateEvent(AssetUpdateEvent.EventAction.Rename, inspectAsset.Id, name));
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
}