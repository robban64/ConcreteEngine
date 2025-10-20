using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Tools.DebugInterface;

public readonly record struct DebugFrameRenderMetric(long FrameIndex, float Fps, float Alpha);
public readonly record struct DebugGfxFrameMetric(int TriangleCount, int DrawCalls);
public readonly record struct DebugGfxStoreMetric(int GfxStoreCount, int BkStoreCount, int GfxStoreFree, int BkFree);
public sealed class DebugDataContainer
{
    public DebugFrameRenderMetric FrameMetric { get; set; }
    public DebugGfxFrameMetric GfxFrameMetric { get; set; }
    public int Entities { get; set; }
    public (int, int) ShadowMap { get; set; }
    public (int, int) Materials { get; set; }
    public Dictionary<string, DebugGfxStoreMetric> GfxStoreMetrics { get; } = new(8);
    public Dictionary<string, (int, int)> AssetMetrics { get; } = new(8);
}

public sealed class DebugInterfaceService
{
    private readonly ImGuiController _controller;

    public DebugDataContainer Data { get; } = new();

    public DebugInterfaceService(GL gl, IWindow window, IInputContext inputCtx)
        => _controller = new ImGuiController(gl, window, inputCtx);

    public void Dispose() => _controller.Dispose();

    public bool BlockInput()
    {
        var io = ImGui.GetIO();
        return io.WantCaptureKeyboard || io.WantCaptureMouse;
    }

    public void Update(float delta) => _controller.Update(delta);

    public void Render()
    {
        var vp = ImGui.GetMainViewport();
        DrawLeft(200);
        DrawRight(200);
        _controller.Render();
    }

    public void DrawLeft(int width)
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(width, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 8f));
        ImGui.Begin("##LeftSidebar",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus);

        DrawSceneMetrics();
        DrawAssetStoreTable();
        DrawGfxStoreTable();

        ImGui.End();
        ImGui.PopStyleVar();
    }

    public void DrawRight(int width)
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(new Vector2(vp.WorkPos.X + vp.WorkSize.X, vp.WorkPos.Y),
            ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.SetNextWindowSize(new Vector2(width, 0f));

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 8f));
        ImGui.Begin("##RightSidebar",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus);

        DrawCpuMetrics();
        DrawGpuMetrics();

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void DrawCpuMetrics()
    {
        ImGui.TextUnformatted("CPU Metrics");
        ImGui.Separator();
        ImGui.TextUnformatted($"Frame Index: {Data.FrameMetric.FrameIndex} ms");
        ImGui.TextUnformatted($"FPS: {Format(Data.FrameMetric.Fps)}");
        ImGui.TextUnformatted($"Alpha: {Format(Data.FrameMetric.Alpha)} ms");
        ImGui.Separator();
    }

    private void DrawGpuMetrics()
    {
        ImGui.TextUnformatted("GPU Metrics");
        ImGui.Separator();
        ImGui.TextUnformatted($"Verts: {Data.GfxFrameMetric.TriangleCount}");
        ImGui.TextUnformatted($"Draws: {Data.GfxFrameMetric.DrawCalls}");
        ImGui.Separator();
    }

    private void DrawSceneMetrics()
    {
        ImGui.TextUnformatted("Scene Metrics");
        ImGui.Separator();
        ImGui.TextUnformatted($"Entities: {Data.Entities}");
        ImGui.TextUnformatted($"ShadowMap: {Data.ShadowMap.Item1}({Data.ShadowMap.Item2})");
        ImGui.Separator();
    }

    private void DrawAssetStoreTable()
    {
        ImGui.TextUnformatted("Asset Store");
        ImGui.Separator();

        if (Data.AssetMetrics.Count == 0)
        {
            ImGui.TextDisabled("No asset metas");
            return;
        }

        if (ImGui.BeginTable("asset_store_tbl", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("Files", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            foreach (var (type,(count, fileCount)) in Data.AssetMetrics)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(type);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(count.ToString());

                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted(fileCount.ToString());
            }

            ImGui.EndTable();
        }

        ImGui.Separator();
    }


    private void DrawGfxStoreTable()
    {
        ImGui.TextUnformatted("GFX Store");
        ImGui.Separator();
        if (ImGui.BeginTable("gfx_metrics_table", 3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Kind", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Gfx", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("BK", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableHeadersRow();

            var dict = Data.GfxStoreMetrics;
            foreach (var (k, v) in dict)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(k);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted($"{v.GfxStoreCount}({v.GfxStoreFree})");

                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted($"{v.BkStoreCount}({v.BkFree})");
            }

            ImGui.EndTable();
        }

        ImGui.Separator();
    }

    private static string Format(float value) => value.ToString("0.00");
}