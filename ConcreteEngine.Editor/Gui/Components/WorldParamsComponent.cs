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


    private static int _editedField = -1;

    private static void OnSelectionChange(WorldParamSelection selection)
    {
        if (selection == ViewModel.Selection) return;
        Model.TriggerEvent(EventKey.SelectionChanged, selection);
    }

    private static void OnSelectionUpdate() => Model.TriggerEvent(EventKey.SelectionUpdated);

    public static void Draw()
    {
        _editedField = -1;

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

        ImGui.Dummy(new Vector2(0, 2));

        //GuiTheme.RightSidebarWidth-12f*2
        if (ImGui.BeginChild("##right-sidebar-world-data", new Vector2(0, 0),
                ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AlwaysUseWindowPadding))
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

        if (_editedField >= 0)
        {
            OnSelectionUpdate();
            _editedField = -1;
        }
    }

    private static void DrawLightState()
    {
        ref var dirLight = ref ViewModel.LightState.DirectionalLight;
        ref var ambientLight = ref ViewModel.LightState.AmbientLight;

        var fieldStatus = new ImGuiFieldStatus();

        ImGui.SeparatorText("Directional Light");

        ImGui.TextUnformatted("Direction");
        ImGui.DragFloat3("##LightDirection", ref dirLight.Direction, 0.01f, -1f, 1f, "%.2f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Diffuse");
        ImGui.ColorEdit3("##LightDiffuse", ref dirLight.Diffuse);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Intensity");
        ImGui.DragFloat("##LightIntensity", ref dirLight.Intensity, 0.01f, 0.0f, 10.0f, "%.3f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Specular");
        ImGui.DragFloat("##LightSpecular", ref dirLight.Specular, 0.01f, 0.0f, 10.0f, "Spec: %.3f");
        fieldStatus.NextFieldDrag();

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Ambient Light");

        ImGui.TextUnformatted("Ambient");
        ImGui.ColorEdit3("##LightAmbient", ref ambientLight.Ambient);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Ambient Ground");
        ImGui.ColorEdit3("##LightAmbientGround", ref ambientLight.AmbientGround);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Exposure");
        ImGui.DragFloat("##LightExposure", ref ambientLight.Exposure, 0.01f, 0.0f, 2.0f, "Exp: %.3f");
        fieldStatus.NextFieldDrag();

        //if (fieldStatus.ActiveField >= 0) Console.WriteLine(fieldStatus.ActiveField);
        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }

    private static void DrawFogState()
    {
        var fieldStatus = new ImGuiFieldStatus();

        ref var fog = ref ViewModel.FogState;
        ImGui.SeparatorText("Fog Details");
        ImGui.ColorEdit3("##FogColor", ref fog.Color);
        fieldStatus.NextFieldDrag();
        ImGui.TextUnformatted("Density");
        ImGui.DragFloat("##FogDensity", ref fog.Density, 1f, 100, 1500, "Den: %.5f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Base Height");
        ImGui.DragFloat("##FogBaseHeight", ref fog.BaseHeight, 1f, -1000f, 1000f, "Hgt: %.1f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Height Falloff");
        ImGui.DragFloat("##FogHeightFalloff", ref fog.HeightFalloff, 1f, 0.001f, 10000.0f, "Fall: %.3f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Height Influence");
        ImGui.DragFloat("##FogHeightInfluence", ref fog.HeightInfluence, 0.001f, 0f, 1f, "Inf: %.3f");
        fieldStatus.NextFieldDrag();

        ImGui.SeparatorText("Fog Optics");
        ImGui.TextUnformatted("Scattering");
        ImGui.DragFloat("##FogScattering", ref fog.Scattering, 0.001f, 0.0f, 1.0f, "Sct: %.3f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Max Distance");
        ImGui.DragFloat("##FogMaxDistance", ref fog.MaxDistance, 1f, 1f, 10000f, "Dis: %.0f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Strength");
        ImGui.DragFloat("##FogStrength", ref fog.Strength, 0.001f, 0f, 1f, "Str: %.3f");
        fieldStatus.NextFieldDrag();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }

    private static void DrawPostEffects()
    {
        var fieldStatus = new ImGuiFieldStatus();

        ref var post = ref ViewModel.PostState;
        ImGui.BeginGroup();
        ImGui.SeparatorText("Grade");
        ImGui.SliderFloat("##GrExposure", ref post.Grade.Exposure, 0f, 1f, "Exp: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##GrSaturation", ref post.Grade.Saturation, 0f, 1f, "Sat: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##GrContrast", ref post.Grade.Contrast, 0f, 1f, "Con: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##GrWarmth", ref post.Grade.Warmth, 0f, 1f, "War: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("White Balance");
        ImGui.SliderFloat("##WbTint", ref post.WhiteBalance.Tint, 0f,1f, "Tint: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##WbStrength", ref post.WhiteBalance.Strength, -1f, 1f, "Str: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Bloom");
        ImGui.SliderFloat("##BlIntensity", ref post.Bloom.Intensity, 0f, 1f, "Int: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##BlThreshold", ref post.Bloom.Threshold, 0f, 1f, "Thr: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##BlRadius", ref post.Bloom.Radius, 0f, 10f, "Rad: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Image FX");
        ImGui.SliderFloat("##FxVignette", ref post.ImageFx.Vignette, -1f, 1f, "Vig: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##FxGrain", ref post.ImageFx.Grain, 0f, 1f, "Grn: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##FxSharpen", ref post.ImageFx.Sharpen, -1f, 1f, "Shr: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##FxRolloff", ref post.ImageFx.Rolloff, 0f, 1f, "Rol: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }


    private static void DrawSelector()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 4));
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

        ImGui.PopStyleVar(1);
    }
}