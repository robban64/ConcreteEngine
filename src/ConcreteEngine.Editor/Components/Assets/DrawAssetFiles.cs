using System.Numerics;
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
    private  Widgets.Popup _popup = new (component.PopupInput,new Vector2(12f, 10f));
    public void Draw(AssetId asset, AssetState state, ref ZaUtf8SpanWriter za)
    {
        var popupId = za.Append(asset).AsSpan();

        if (ImGui.ArrowButton("<"u8, ImGuiDir.Left))
        {

            var itemMin = ImGui.GetItemRectMin();
            var itemMax = ImGui.GetItemRectMin();
            ImGui.SetNextWindowPos(new Vector2(itemMax.X + 16, itemMin.Y - 32));
            ImGui.OpenPopup(popupId);
        }

        if (ImGui.IsPopupOpen(popupId))
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f));


        if (ImGui.BeginPopup(popupId))
        {
            DrawFilesTable(state.Proxy!.FileSpecs, ref za);
            ImGui.EndPopup();
            ImGui.PopStyleVar();
        }

        za.Clear();
    }

    private void DrawFilesTable(AssetFileSpec[] fileSpecs, ref ZaUtf8SpanWriter za)
    {
        GuiUtils.DrawSectionHeader("Files"u8);
        if (!ImGui.BeginTable("##asset_store_files_tbl"u8, 4, ImGuiTableFlags.Borders)) return;

        ImGui.TableSetupColumn("ID"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Path"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Size"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Hash"u8, ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableHeadersRow();

        foreach (var it in fileSpecs)
        {
            za.Clear();
            ImGui.TableNextRow();
            ImGui.PushID(za.AppendEnd(it.Id.Value).AsSpan());
            za.Clear();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(za.AppendEnd(it.Id.Value).AsSpan());
            za.Clear();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(za.AppendEnd(it.RelativePath).AsSpan());
            za.Clear();

            ImGui.TableNextColumn();
            ImGui.TextUnformatted(za.AppendEnd(it.SizeBytes).AsSpan());
            za.Clear();

            ImGui.TableNextColumn();
            if (it.ContentHash != null)
                ImGui.Text(za.AppendEnd(it.ContentHash).AsSpan());

            ImGui.PopID();
        }

        za.Clear();

        ImGui.EndTable();
    }
}