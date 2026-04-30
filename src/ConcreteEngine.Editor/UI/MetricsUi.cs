using System.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.Assets.Extensions;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Graphics.Gfx.Utility;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static class MetricsUi
{
    private const ImGuiTableFlags TableFlags =
        ImGuiTableFlags.PadOuterX | ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.SizingFixedFit;

    private static MetricSystem Metrics => MetricSystem.Instance;

    private static int _selected = 0;
    private static int _popupInput = 1;

    public static void Draw()
    {
        ImGui.SetNextWindowSizeConstraints(new Vector2(300, 300), new Vector2(800, 800));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12, 12));
        if (!ImGui.Begin("metric-window"u8, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.MenuBar))
        {
            ImGui.End();
            ImGui.PopStyleVar();
            return;
        }

        if (ImGui.BeginMenuBar())
        {
            if (ImGui.MenuItem("Frame"u8, _selected == 0)) _selected = 0;
            if (ImGui.MenuItem("Scene"u8, _selected == 1)) _selected = 1;
            if (ImGui.MenuItem("Store"u8, _selected == 2)) _selected = 2;

            ImGui.EndMenuBar();
        }

        switch (_selected)
        {
            case 0: DrawFrameMetrics(); break;
            case 1: DrawSceneMetrics(); break;
            case 2: DrawStoreMetrics(); break;
            default: ImGui.TextUnformatted("Invalid Selection"u8); break;
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private static void DrawSceneMetrics()
    {
        var sw = TextBuffers.GetWriter();
        if (ImGui.BeginChild("metrics-scene"u8, ImGuiChildFlags.AutoResizeY))
        {
            ref readonly var scene = ref Metrics.SceneMeta;
            AppDraw.DrawTextProperty("SceneObjects: "u8, sw.Write(scene.SceneObjects));
            AppDraw.DrawTextProperty("Visible Entities: "u8, sw.Write(scene.VisibleEntities));

            AppDraw.DrawTextProperty("RenderEcs: "u8, sw.Write(scene.RenderEcs));
            AppDraw.DrawSameLineProperty();
            AppDraw.DrawTextProperty("GameEcs: "u8, sw.Write(scene.GameEcs));
        }

        ImGui.EndChild();
    }


    private static void DrawFrameMetrics()
    {
        ref readonly var frameMeta = ref Metrics.FrameMeta;
        var sw = TextBuffers.GetWriter();
        // Frame Info
        ImGui.SeparatorText("Frame Info"u8);
        MetricText(sw, "Frame:", frameMeta.FrameId);

        AppDraw.TextU8("FPS:"u8);
        ImGui.SameLine();
        AppDraw.Text(sw.Append(frameMeta.Fps, "F2").Append(" (").Append(frameMeta.Alpha, "F2")
            .Append("ms)").End());

        // Render Frame 
        ref readonly var gpu = ref Metrics.GpuFrameMeta;
        ImGui.SeparatorText("Render Info"u8);
        MetricText(sw, "Draws:", gpu.Frame.Draws);
        MetricText(sw, "Tris:", gpu.Frame.Tris);
        ImGui.Spacing();
        MetricText(sw, "VBO Uploaded:", gpu.Buffer.MeshBufferBytes, space: 0);
        MetricText(sw, "UBO Uploaded:", gpu.Buffer.UniformBufferBytes, space: 0);

        // Frame Metric
        ref readonly var frameMetric = ref Metrics.Metric;
        ImGui.SeparatorText("Frame Metric"u8);
        MetricText(sw, "Avg:", frameMetric.AvgMs, format: "F4", suffix: "ms");
        MetricText(sw, "Max:", frameMetric.MaxMs, format: "F4", suffix: "ms");
        MetricText(sw, "Min:", frameMetric.MinMs, format: "F4", suffix: "ms");

        ImGui.Dummy(new Vector2(0, 2));

        // Gc Metric
        ImGui.SeparatorText("Runtime Metric"u8);
        MetricText(sw, "Compiled IL:", frameMetric.CompiledILKb, suffix: "KB", space: 80);
        MetricText(sw, "Allocated:", frameMetric.AllocatedMb, suffix: "MB", space: 70);
        MetricText(sw, "AllocRate:", frameMetric.AllocMbPerSec, format: "F4", suffix: "MB/s", space: 70);

        var status = frameMetric.GcActivity switch
        {
            GcActivity.None => "Idle",
            GcActivity.Minor => "Minor",
            GcActivity.Major => "Major",
            _ => "-"
        };
        AppDraw.Text(sw.Append("Status: ["u8).Append(status).Append(']').End());
        ImGui.SameLine();
        AppDraw.Text(
            sw.Append("Gen: "u8).Append('[')
                .Append(frameMetric.Gc.Gen0).Append(", "u8)
                .Append(frameMetric.Gc.Gen1).Append(", "u8)
                .Append(frameMetric.Gc.Gen2).Append(']').End()
        );
    }

    private static void DrawStoreMetrics()
    {
        if (Metrics.Stores is not { } stores) return;

        var sw = TextBuffers.GetWriter();

        if (ImGui.BeginChild("metrics-asset"u8, ImGuiChildFlags.AutoResizeY))
        {
            ImGui.SeparatorText("Asset Metrics"u8);

            ImGui.BeginTable("asset_store_tbl"u8, 3, TableFlags);
            ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthStretch, 1.00f);
            ImGui.TableSetupColumn("Count"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
            ImGui.TableSetupColumn("Files"u8, ImGuiTableColumnFlags.WidthStretch, 0.35f);
            ImGui.TableHeadersRow();

            for (var i = 0; i < stores.Assets.Length; i++)
            {
                var it = stores.Assets[i];
                ImGui.PushID(i);
                ImGui.TableNextRow();
                AppDraw.TextColumn(sw.Write(it.Kind.ToText()));
                AppDraw.TextColumn(sw.Write(it.Count));
                AppDraw.TextColumn(sw.Write(it.FileCount));
                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        ImGui.Dummy(new Vector2(0, 6));

        ImGui.EndChild();

        if (ImGui.BeginChild("metrics-gfx"u8, ImGuiChildFlags.AutoResizeY))
        {
            ImGui.SeparatorText("Gfx Metrics"u8);

            ImGui.BeginTabBar("metrics_tabs"u8, ImGuiTabBarFlags.FittingPolicyScroll);
            if (ImGui.BeginTabItem("Main"u8))
            {
                DrawGraphicsTable(stores, false);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Backend"u8))
            {
                DrawGraphicsTable(stores, true);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
            ImGui.Dummy(new Vector2(0, 2));
        }

        ImGui.EndChild();
    }


    private static void DrawGraphicsTable(StoreMetrics store, bool bkStore)
    {
        int cols = bkStore ? 3 : 4;
        if (!ImGui.BeginTable(bkStore ? "bk-table"u8 : "gfx-table"u8, cols, TableFlags)) return;

        ImGui.TableSetupColumn("##Name"u8, ImGuiTableColumnFlags.WidthFixed, 26f);
        ImGui.TableSetupColumn("Cnt/Free"u8, ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("Live/Cap"u8, ImGuiTableColumnFlags.WidthStretch, 0.8f);
        if (!bkStore) ImGui.TableSetupColumn("*"u8, ImGuiTableColumnFlags.WidthStretch, 1f);

        ImGui.TableHeadersRow();
        if (bkStore) DrawBkStore(store);
        else DrawGfxStore(store);
        ImGui.EndTable();
    }

    private static void DrawBkStore(StoreMetrics storeMetrics)
    {
        var metas = storeMetrics.Gfx;
        var sw = TextBuffers.GetWriter();
        for (int i = 0; i < metas.Length; i++)
        {
            ref readonly var it = ref metas[i];
            ImGui.PushID(i);
            ImGui.TableNextRow();
            AppDraw.TextColumn(sw.Write(it.Kind.ToShortText()));
            AppDraw.TextColumn(sw.Append(it.Bk.Count).Append('/').Append(it.Bk.Reserved).End());
            AppDraw.TextColumn(sw.Append(it.Bk.Active).Append('/').Append(it.Bk.Capacity).End());
            ImGui.PopID();
        }
    }

    private static void DrawGfxStore(StoreMetrics storeMetrics)
    {
        var metas = storeMetrics.Gfx;
        var descriptions = storeMetrics.GfxMetaDescriptions;
        ArgumentOutOfRangeException.ThrowIfNotEqual(metas.Length, descriptions.Length);

        var sw = TextBuffers.GetWriter();
        for (int i = 0; i < metas.Length; i++)
        {
            ref readonly var it = ref metas[i];

            ImGui.PushID(i);
            ImGui.TableNextRow();

            var open = ImGui.Selectable("##row"u8, false,
                ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

            AppDraw.TextColumn(sw.Write(it.Kind.ToShortText()));
            AppDraw.TextColumn(sw.Append(it.Fk.Count).Append('/').Append(it.Fk.Reserved).End());
            AppDraw.TextColumn(sw.Append(it.Fk.Active).Append('/').Append(it.Fk.Capacity).End());
            AppDraw.TextColumn(sw.Write(descriptions[i]));

            DrawPopup(open);

            ImGui.PopID();
        }

        return;

        static void DrawPopup(bool isOpen)
        {
            if (isOpen)
            {
                if (_popupInput < 1) _popupInput = 1;
                ImGui.OpenPopup("row_popup"u8);
            }

            if (ImGui.BeginPopup("row_popup"u8))
            {
                ImGui.TextUnformatted("Id"u8);
                ImGui.SameLine();
                var popupId = _popupInput;
                if (ImGui.InputInt("##Idu8", ref popupId)) _popupInput = popupId;
                if (_popupInput < 1) _popupInput = 1;

                var canPrint = _popupInput >= 1;
                if (!canPrint) ImGui.BeginDisabled();
                if (ImGui.Button("Print"u8))
                {
                    ImGui.CloseCurrentPopup();
                }

                if (!canPrint) ImGui.EndDisabled();

                ImGui.EndPopup();
            }
        }
    }


    private static void MetricText(
        UnsafeSpanWriter sw,
        string prefix,
        float value,
        string format = "",
        string suffix = "",
        float space = 50)
    {
        AppDraw.Text(sw.Append(prefix).End());

        if (space == 0) ImGui.SameLine();
        else ImGui.SameLine(space);
        AppDraw.Text(sw.Append(value, format).Append(suffix).End());
    }

    private static void MetricText(
        UnsafeSpanWriter sw,
        string prefix,
        Half value,
        string format = "",
        string suffix = "",
        float space = 50)
    {
        AppDraw.Text(sw.Append(prefix).End());

        if (space == 0) ImGui.SameLine();
        else ImGui.SameLine(space);
        AppDraw.Text(sw.Append(value, format).Append(suffix).End());
    }
}