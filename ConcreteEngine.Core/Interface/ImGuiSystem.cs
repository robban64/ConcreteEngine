using System.Numerics;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Utility;
using ConcreteEngine.Renderer.State;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace ConcreteEngine.Core.Interface;

public record struct DebugFrameStats(
    in RenderFrameInfo FrameInfo,
    in GfxFrameResult FrameResult,
    int Entities);

public sealed class GuiDebugModel
{
    public RenderFrameInfo FrameInfo { get; set; }
    public GfxFrameResult GfxResult { get; set; }
    public int Entities { get; set; }
    public Dictionary<Type, AssetTypeMetaSnapshot> AssetMetas { get; } = new(8);
}

internal sealed class ImGuiSystem : IDisposable
{
    private readonly ImGuiController _controller;

    private readonly GuiDebugModel _model = new();

    private DebugFrameStats _nextStats;

    public ImGuiSystem(GL gl, IWindow window, IInputContext inputCtx)
        => _controller = new ImGuiController(gl, window, inputCtx);

    public void Dispose() => _controller.Dispose();


    public bool BlockInput()
    {
        var io = ImGui.GetIO();
        return io.WantCaptureKeyboard || io.WantCaptureMouse;
    }

    public void RefreshAsset(IReadOnlyDictionary<Type, AssetTypeMeta> assetMetas)
    {
        _model.AssetMetas.Clear();
        foreach (var (k, v) in assetMetas)
            _model.AssetMetas.Add(k, v.ToSnapshot());
    }

    public void RefreshStats()
    {
        _model.FrameInfo = _nextStats.FrameInfo;
        _model.GfxResult = _nextStats.FrameResult;
        _model.Entities = _nextStats.Entities;
    }

    public void Update(float delta) => _controller.Update(delta);

    public void Render(in DebugFrameStats stats)
    {
        _nextStats = stats;

        var vp = ImGui.GetMainViewport();
        DrawLeft(200);
        DrawRight(200);
        _controller.Render();
    }

    public void DrawLeft(int width)
    {
        var vp = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(vp.WorkPos);
        ImGui.SetNextWindowSize(new Vector2(width, 0f)); // auto height

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
        ImGui.TextUnformatted($"FPS: {Format(_model.FrameInfo.Fps)}");
        ImGui.TextUnformatted($"Delta Time: {Format(_model.FrameInfo.DeltaTime)} ms");
        ImGui.Separator();
        ImGui.TextUnformatted($"Frame Index: {_model.FrameInfo.FrameIndex} ms");
    }

    private void DrawGpuMetrics()
    {
        ImGui.TextUnformatted("GPU Metrics");
        ImGui.Separator();
        ImGui.TextUnformatted($"Verts: {_model.GfxResult.TriangleCount}");
        ImGui.TextUnformatted($"Draws: {_model.GfxResult.DrawCalls}");
        ImGui.Separator();
    }

    private void DrawSceneMetrics()
    {
        ImGui.TextUnformatted("Scene Metrics");
        ImGui.Separator();
        ImGui.TextUnformatted($"Entities: {_model.Entities}");
        ImGui.Separator();
    }

    private void DrawAssetStoreTable()
    {
        ImGui.TextUnformatted("Asset Store");
        ImGui.Separator();

        if (_model.AssetMetas.Count == 0)
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

            foreach (var (type, meta) in _model.AssetMetas)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(type.Name.AsSpan(0, int.Min(8, type.Name.Length - 1)));

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted(meta.Count.ToString());

                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted(meta.FileCount.ToString());
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

            var dict = GfxDebugMetrics.GetStoreMetrics();
            foreach (var kv in dict)
            {
                var kind = kv.Key.ToString();
                var m = kv.Value;

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(kind.AsSpan(0, int.Min(8, kind.Length - 1)));

                ImGui.TableSetColumnIndex(1);
                ImGui.TextUnformatted($"{m.GfxStoreCount}({m.GfxStoreFree})");

                ImGui.TableSetColumnIndex(2);
                ImGui.TextUnformatted($"{m.BackendStoreCount}({m.BackendStoreFree})");
            }

            ImGui.EndTable();
        }

        ImGui.Separator();
    }

    private static string Format(float value) => value.ToString("0.00");
}