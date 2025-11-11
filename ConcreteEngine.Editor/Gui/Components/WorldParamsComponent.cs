using System.Numerics;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Editor.ViewModel;
using ImGuiNET;

namespace ConcreteEngine.Editor.Gui.Components;

internal static class WorldParamsComponent
{
    private static ModelState<WorldRenderViewModel> Model => ModelManager.WorldRenderState;
    private static WorldRenderViewModel ViewModel => Model.State!;

    public static ref WorldParamState DataState => ref ViewModel.DataState;
    public static ref LightState LightState => ref ViewModel.DataState.LightState;
    public static ref FogState FogState => ref ViewModel.DataState.FogState;
    public static ref PostEffectState PostState => ref ViewModel.DataState.PostState;

    private static void OnSelectionChange(WorldParamSelection selection)
    {
        if (selection == ViewModel.Selection) return;
        Model.TriggerEvent(EventKey.SelectionChanged, selection);
    }

    private static void OnSelectionUpdate()
    {
        Model.TriggerEvent(EventKey.SelectionUpdated, in DataState);
    }

    public static void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 0));
        if (ImGui.BeginChild("##right-sidebar-world-header", new Vector2(0),
                ImGuiChildFlags.AlwaysUseWindowPadding | ImGuiChildFlags.AutoResizeY))
        {
            ImGui.PopStyleVar();

            ImGui.SeparatorText("World Params");
            ImGui.Separator();

            DrawSelector();

            ImGui.EndChild();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 0));
        //GuiTheme.RightSidebarWidth-12f*2
        if (ImGui.BeginChild("##right-sidebar-world-data", new Vector2(0, 0),
                ImGuiChildFlags.AlwaysAutoResize))
        {
            ImGui.PopStyleVar();

            switch (ViewModel.Selection)
            {
                case WorldParamSelection.Light: DrawLightState(); break;
                case WorldParamSelection.Fog: DrawFogState(); break;
                case WorldParamSelection.PostEffect: DrawPostEffects(); break;
                default: throw new ArgumentOutOfRangeException();
            }

            ImGui.EndChild();
        }
    }

    private static void DrawLightState()
    {
        ref var dirLight = ref ViewModel.LightState.DirectionalLight;
        ref var ambientLight = ref ViewModel.LightState.AmbientLight;


        if (ImGui.Button("Update")) OnSelectionUpdate();

        ImGui.SeparatorText("Directional Light");
        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted("Direction");
        var update = ImGui.DragFloat3("##LightDirection", ref dirLight.Direction, 0.01f, -1f, 1f, "%.2f");

        ImGui.TextUnformatted("Diffuse");
        update |= ImGui.ColorEdit3("##LightDiffuse", ref dirLight.Diffuse);

        ImGui.TextUnformatted("Intensity");
        update |= ImGui.DragFloat("##LightIntensity", ref dirLight.Intensity, 0.01f, 0.0f, 10.0f, "%.3f");

        ImGui.TextUnformatted("Specular");
        update |= ImGui.DragFloat("##LightSpecular", ref dirLight.Specular, 0.01f, 0.0f, 10.0f, "Spec: %.3f");

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Ambient Light");
        ImGui.Dummy(new Vector2(0, 2));

        ImGui.TextUnformatted("Ambient");
        update |= ImGui.ColorEdit3("##LightAmbient", ref ambientLight.Ambient);

        ImGui.TextUnformatted("Ambient Ground");
        update |= ImGui.ColorEdit3("##LightAmbientGround", ref ambientLight.AmbientGround);
        ImGui.TextUnformatted("Exposure");
        update |= ImGui.DragFloat("##LightExposure", ref ambientLight.Exposure, 0.01f, 0.0f, 10.0f, "Exp: %.3f");

        if (update)
        {
        }
    }

    private static void DrawFogState()
    {
        ref var fog = ref ViewModel.FogState;

        ImGui.SeparatorText("Fog Details");
        ImGui.ColorEdit3("##FogColor", ref fog.Color);
        ImGui.DragFloat("##FogDensity", ref fog.Density, 0.0001f, 0.0001f, 1.0f, "Dens: %.5f");
        ImGui.DragFloat("##FogBaseHeight", ref fog.BaseHeight, 0.1f, -1000f, 1000f, "BHeight: %.1f");
        ImGui.DragFloat("##FogHeightFalloff", ref fog.HeightFalloff, 0.001f, 0.0f, 10.0f, "HFall: %.3f");
        ImGui.DragFloat("##FogHeightInfluence", ref fog.HeightInfluence, 0.001f, 0f, 1f, "HInf: %.3f");


        ImGui.SeparatorText("Fog Optics");
        ImGui.DragFloat("##FogScattering", ref fog.Scattering, 0.001f, 0.0f, 1.0f, "Sct: %.3f");
        ImGui.DragFloat("##FogMaxDistance", ref fog.MaxDistance, 1f, 0f, 5000f, "Dist: %.0f");
        ImGui.DragFloat("##FogStrength", ref fog.Strength, 0.001f, 0f, 1f, "Str: %.3f");
    }

    private static void DrawPostEffects()
    {
        ref var post = ref ViewModel.PostState;
        ImGui.SeparatorText("Grade");
        ImGui.SliderFloat("##GrExposure", ref post.Grade.Exposure, -2f, 2f, "Exp: %.2f");
        ImGui.SliderFloat("##GrSaturation", ref post.Grade.Saturation, -1f, 1f, "Sat: %.2f");
        ImGui.SliderFloat("##GrContrast", ref post.Grade.Contrast, -1f, 1f, "Con: %.2f");
        ImGui.SliderFloat("##GrWarmth", ref post.Grade.Warmth, -1f, 1f, "War: %.2f");

        ImGui.SeparatorText("White Balance");
        ImGui.SliderFloat("##WbTint", ref post.WhiteBalance.Tint, -1f, 1f, "Tint: %.2f");
        ImGui.SliderFloat("##WbStrength", ref post.WhiteBalance.Strength, 0f, 2f, "Str: %.2f");

        ImGui.SeparatorText("Bloom");
        ImGui.SliderFloat("##BlIntensity", ref post.Bloom.Intensity, 0f, 3f, "Int: %.2f");
        ImGui.SliderFloat("##BlThreshold", ref post.Bloom.Threshold, 0f, 1f, "Thr: %.2f");
        ImGui.SliderFloat("##BlRadius", ref post.Bloom.Radius, 0f, 10f, "Rad: %.2f");


        ImGui.SeparatorText("Image FX");
        ImGui.SliderFloat("##FxVignette", ref post.ImageFx.Vignette, 0f, 1f, "Vig: %.2f");
        ImGui.SliderFloat("##FxGrain", ref post.ImageFx.Grain, 0f, 1f, "Grn: %.2f");
        ImGui.SliderFloat("##FxSharpen", ref post.ImageFx.Sharpen, 0f, 1f, "Shr: %.2f");
        ImGui.SliderFloat("##FxRolloff", ref post.ImageFx.Rolloff, 0f, 1f, "Rol: %.2f");
    }


    private static void DrawSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 0.5f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabBarBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabBorderSize, 1);
        ImGui.PushStyleColor(ImGuiCol.TabHovered, GuiTheme.Blue1);
        ImGui.PushStyleColor(ImGuiCol.TabActive, GuiTheme.SelectedColor);
        ImGui.PushStyleColor(ImGuiCol.Tab, GuiTheme.PrimaryColor);
        if (ImGui.BeginTabBar("world-selection-tabs", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("Light"))
            {
                OnSelectionChange(WorldParamSelection.Light);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Fog"))
            {
                OnSelectionChange(WorldParamSelection.Fog);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Post"))
            {
                OnSelectionChange(WorldParamSelection.PostEffect);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar(4);
        ImGui.PopStyleColor(3);
    }
}