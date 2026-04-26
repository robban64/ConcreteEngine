using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Metrics;

internal sealed unsafe class MetricsLeftPanel(StateManager state) : EditorPanel(PanelId.MetricsLeft, state)
{
    private readonly MetricSystem _metricSystem = MetricSystem.Instance;

    public override void OnEnter(ref MemoryBlockPtr memory)
    {
        MetricSystem.Instance.FastMode = true;
        MetricSystem.Instance.Stores?.Refresh();
    }

    public override void OnLeave()
    {
        MetricSystem.Instance.FastMode = false;
    }

    public override void OnDraw()
    {
        var sw = TextBuffers.GetWriter();
        
        if (ImGui.BeginChild("metrics-scene"u8, ImGuiChildFlags.AutoResizeY))
        {
            ref readonly var scene = ref _metricSystem.SceneMeta;
            AppDraw.DrawTextProperty("SceneObjects: "u8, sw.Write(scene.SceneObjects));
            AppDraw.DrawTextProperty("Visible Entities: "u8, sw.Write(scene.VisibleEntities));

            AppDraw.DrawTextProperty("RenderEcs: "u8, sw.Write(scene.RenderEcs));
            AppDraw.DrawSameLineProperty();
            AppDraw.DrawTextProperty("GameEcs: "u8, sw.Write(scene.GameEcs));
        }

        ImGui.EndChild();

        if (MetricSystem.Instance.Stores is not { } stores) return;
        if (ImGui.BeginChild("metrics-asset"u8, ImGuiChildFlags.AutoResizeY))
        {
            DrawAssetStoreMetrics.Draw( stores.Assets);
        }

        ImGui.EndChild();

        ImGui.Dummy(new Vector2(0, 6));

        if (ImGui.BeginChild("metrics-gfx"u8, ImGuiChildFlags.AutoResizeY))
        {
            DrawGfxStoreMetrics.Draw(stores.Gfx, stores.GfxMetaDescriptions);
        }

        ImGui.EndChild();
    }
}

internal sealed class MetricsRightPanel(StateManager state) : EditorPanel(PanelId.MetricsRight, state)
{
    private GcActivity _gcActivity;
    private float _gcCooldown;

    public override void OnDraw()
    {
        ImGui.PushID("metrics-right"u8);

        // TickGcActivity(EditorTime.DeltaTime, MetricSystem.Instance.RuntimeMetric.GcActivity);

        DrawSystemMetrics.DrawFrameMeta();
        DrawSystemMetrics.DrawPerformanceMetrics();
        /*
        ImGui.Dummy(new Vector2(0, 4));
        DrawSystemMetrics.DrawSession(ctx, MetricSystem.Instance.RuntimeMetric.AllocMbPerSec);
        ImGui.Dummy(new Vector2(0, 4));
        DrawSystemMetrics.DrawFooter();
        */

        ImGui.PopID();
    }

    private void TickGcActivity(float delta, GcActivity activity)
    {
        if (_gcActivity == GcActivity.None && activity == GcActivity.None) return;

        if (_gcActivity != activity)
        {
            _gcActivity = activity;
            _gcCooldown = 4;
        }

        _gcCooldown -= delta;
        if (_gcCooldown <= 0)
        {
            _gcActivity = GcActivity.None;
            _gcCooldown = 0;
        }
    }
}