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

internal sealed class AssetInspectorPanel(PanelContext context, AssetController assetController)
    : EditorPanel(PanelId.AssetProperty, context)
{
    private const int StringNameCapacity = 64;
    private const int NameBufferCapacity = 128;
    private static readonly byte[] NameInputBuffer = new byte[NameBufferCapacity];

    private readonly TextureInspectorUi _textureProxyUi = new(context, assetController);
    private readonly MaterialInspectorUi _materialProxyUi = new(context, assetController);
    private readonly ShaderInspectorUi _shaderInspectorUi = new(context, assetController);
    private readonly ModelInspectorUi _modelInspectorUi = new(context, assetController);

    private Popup _popup = new(new Vector2(12f, 10f));

    public override void Enter()
    {
    }

    public override void Draw(in FrameContext ctx)
    {
        if (Context.Selection.SelectedAsset is not { } editorAsset) return;

        ImGui.PushID(editorAsset.Asset.Id);
        DrawHeader(editorAsset, ctx);
        ImGui.Spacing();
        ImGui.Separator();

        switch (editorAsset)
        {
            case InspectShader shader:
                _shaderInspectorUi.Draw(shader, in ctx);
                break;
            case InspectModel model:
                _modelInspectorUi.Draw(model, in ctx);
                break;
            case InspectTexture texture:
                _textureProxyUi.Draw(texture, in ctx);
                break;
            case InspectMaterial material:
                _materialProxyUi.Draw(material, in ctx);
                break;
        }

        ImGui.PopID();
    }

    private unsafe void DrawHeader(InspectAsset inspectAsset, FrameContext ctx)
    {
        const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll;
        var asset = inspectAsset.Asset;

        ImGui.BeginGroup();
        {
            GuiTheme.PushFontIconText();
            if (ImGui.Button(ctx.WriteIcon(IconNames.File))) _popup.State = true;
            ImGui.PopFont();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetAssetColor(asset.Kind));
            ImGui.SeparatorText(ref ctx.Sw.Start(asset.Kind.ToText()).Append(" - ["u8).Append(asset.Id).Append(':')
                .Append(asset.Generation).Append(']').End());
            ImGui.PopStyleColor();
        }
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        {
            AppDraw.DrawIcon(ctx.WriteIcon(inspectAsset.GetIcon()));
            ImGui.SameLine();
            //ref var name = ref ctx.Sw.Write(asset.Name);
            ref var buffer = ref MemoryMarshal.GetArrayDataReference(NameInputBuffer);
            if (ImGui.InputText("##name"u8, ref buffer, NameBufferCapacity, inputFlags))
            {
                HandleRename();
            }
        }
        ImGui.EndGroup();

        var pos = new Vector2(ImGui.GetItemRectMin().X - 200, ImGui.GetItemRectMin().Y - 50);
        if (_popup.Begin("asset-file-specs"u8, pos))
        {
            DrawFilesTable(inspectAsset.FileSpecs, ctx.Sw);
            _popup.End();
        }
    }

    private static void HandleRename()
    {
        UtfText.SliceNullTerminate(NameInputBuffer, out var byteSpan);
        var charLength = Encoding.UTF8.GetCharCount(NameInputBuffer);
        charLength = int.Min(charLength, StringNameCapacity);
        if (charLength <= 0) return;

        Span<char> charSpan = stackalloc char[charLength];

        int len = Encoding.UTF8.GetChars(byteSpan, charSpan);
        var nameString = charSpan.Length == len ? charSpan : charSpan.Slice(0, len);

        nameString = nameString.Trim();
        Console.WriteLine(nameString);
        // rename
    }

    private static void DrawFilesTable(AssetFileSpec[] fileSpecs, UnsafeSpanWriter sw)
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
            layout.Column(ref sw.Write(it.Id.Value));
            layout.Column(ref sw.Write(it.RelativePath));
            layout.Column(ref sw.Write(it.SizeBytes));
            layout.Column(ref sw.Write(it.ContentHash ?? ""));
            ImGui.PopID();
        }

        ImGui.EndTable();
    }
}