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

    private AssetId _previousId =  AssetId.Empty;

    public override void Enter()
    {
    }

    public override void Leave()
    {
        _previousId =  AssetId.Empty;
        Array.Clear(NameInputBuffer);
    }

    private static void RestoreName(InspectAsset asset)
    {
        int len = UtfText.WriteCharSpanSafe(asset.Asset.Name, NameInputBuffer);
        NameInputBuffer[len] = 0;
    }

    public override void Draw(in FrameContext ctx)
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

        ImGui.BeginGroup();
        {
            GuiTheme.PushFontIconText();
            if (ImGui.Button(ctx.WriteIcon(inspectAsset.GetIcon()))) _popup.State = true;
            ImGui.PopFont();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetAssetColor(inspectAsset.Kind));
            ImGui.SeparatorText(ref ctx.Sw.Start(inspectAsset.Kind.ToText()).Append(" - ["u8).Append(inspectAsset.Id).Append(':')
                .Append(inspectAsset.Asset.Generation).Append(']').End());
            ImGui.PopStyleColor();
        }
        ImGui.EndGroup();

        ImGui.Spacing();

        ImGui.BeginGroup();
        {
            GuiTheme.PushFontIconText();
            if (ImGui.Button(ctx.WriteIcon(IconNames.Undo2)))
                RestoreName(inspectAsset);
            ImGui.PopFont();

            ImGui.SameLine();
            ref var buffer = ref MemoryMarshal.GetArrayDataReference(NameInputBuffer);
            if (ImGui.InputText("##name"u8, ref buffer, NameBufferCapacity, inputFlags))
            {
                HandleRename(inspectAsset);
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

    private static void HandleRename(InspectAsset inspectAsset)
    {
        UtfText.SliceNullTerminate(NameInputBuffer, out var byteSpan);
        if (byteSpan.IsEmpty) return;
        
        var charLength = Math.Min(Encoding.UTF8.GetCharCount(byteSpan), StringNameCapacity);
        Span<char> chars = stackalloc char[charLength];
        Encoding.UTF8.GetChars(byteSpan, chars);

        var name = chars.Trim();
        if (name.IsEmpty || name.Equals(inspectAsset.Asset.Name, StringComparison.Ordinal)) return;       
        
        Console.WriteLine($"New name is {name}");
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