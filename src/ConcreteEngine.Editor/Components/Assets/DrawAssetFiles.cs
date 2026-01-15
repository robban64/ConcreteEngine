using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.Components.Assets;

internal sealed class DrawAssetFiles(AssetsComponent component)
{
    private Popup _popup = new(new Vector2(12f, 10f));

    public void Draw(AssetState state, ref ZaUtf8SpanWriter za)
    {
        if (ImGui.ArrowButton("<"u8, ImGuiDir.Left))
            _popup.State = true;

        var pos = new Vector2(ImGui.GetItemRectMin().X - 32, ImGui.GetItemRectMin().Y - 32);
        if (_popup.Begin(state.GetPopupId(), pos))
        {
            DrawFilesTable(state.Proxy!.FileSpecs);
            _popup.End();
        }
    }

    private void DrawFilesTable(AssetFileSpec[] fileSpecs)
    {
        GuiUtils.DrawSectionHeader("Files"u8);
        if (!ImGui.BeginTable("##asset_store_files_tbl"u8, 4, ImGuiTableFlags.Borders)) return;

        ImGui.TableSetupColumn("ID"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Path"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Hash"u8, ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableHeadersRow();

        var draw = component.DrawCtx;
        var write = draw.GetWriter();

        foreach (var it in fileSpecs)
        {
            ImGui.PushID(it.Id.Value);
            ImGui.TableNextRow();
            draw.NextColumn(ref write.AppendEnd(it.Id.Value));
            draw.NextColumn(ref write.AppendEnd(it.RelativePath));
            draw.NextColumn(ref write.AppendEnd(it.SizeBytes));
            draw.NextColumn(ref write.AppendEnd(it.ContentHash ?? ""));
            ImGui.PopID();
        }

        write.Clear();

        ImGui.EndTable();
    }
}