using System.Numerics;
using ConcreteEngine.Core.Specs.Visuals;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.Components;

internal static class WorldParamsComponent
{
    private static int _editedField = -1;

    private static WorldParamSelection _selection;

    private static void OnUpdateShadowSize(int size)
    {
        var existingSize = EditorDataStore.Slot<WorldParamsData>.State.Shadow.ShadowMapSize;
        if (size == existingSize) return;
        ModelManager.WorldRenderStateContext.TriggerEvent(EventKey.WorldActionInvoke, size);
    }

    private static void OnSelectionChange(WorldParamSelection selection) => _selection = selection;


    public static void Draw()
    {
        _editedField = -1;

        if (ImGui.BeginChild("##right-sidebar-world-header", new Vector2(0), ImGuiChildFlags.AutoResizeY))
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

            switch (_selection)
            {
                case WorldParamSelection.Light: DrawLightState(); break;
                case WorldParamSelection.Fog: DrawFogState(); break;
                case WorldParamSelection.Post: DrawPostEffects(); break;
                case WorldParamSelection.Shadow: DrawShadow(); break;

                default: throw new ArgumentOutOfRangeException();
            }

            ImGui.EndChild();
        }

        if (_editedField >= 0)
        {
            EngineController.CommitWorldParams();
            _editedField = -1;
        }
    }


    private static void DrawShadow()
    {
        var fieldStatus = new ImGuiFieldStatus();

        ref var shadow = ref EditorDataStore.Slot<WorldParamsData>.State.Shadow;
        int size = shadow.ShadowMapSize;

        ImGui.BeginGroup();
        ImGui.SeparatorText("Shadow Map Size");
        ImGui.TextUnformatted(new NumberSpanFormatter(StringUtils.CharBuffer8).Format(size));

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 6));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 6));
        if (ImGui.BeginCombo("##shMapSize", "Set Size", ImGuiComboFlags.HeightLargest))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 12));

            if (ImGui.Selectable("1024", size == 1024, 0, default)) OnUpdateShadowSize(1024);
            else if (ImGui.Selectable("2048", size == 2048, 0, default)) OnUpdateShadowSize(2048);
            else if (ImGui.Selectable("4096", size == 4096, 0, default)) OnUpdateShadowSize(4096);
            else if (ImGui.Selectable("8192", size == 8192, 0, default)) OnUpdateShadowSize(8192);


            ImGui.PopStyleVar();
            ImGui.EndCombo();
        }

        ImGui.PopStyleVar(2);

        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Shadow Setting");

        ImGui.TextUnformatted("Distance");
        ImGui.SliderFloat("##ShDist", ref shadow.Distance, 10f, 200f, "%.2f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("ZPad");
        ImGui.SliderFloat("##ShZPad", ref shadow.ZPad, 1f, 200f, "%.2f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("ConstBias");
        ImGui.SliderFloat("##ShConstBias", ref shadow.ConstBias, 0.0001f, 0.001f, "%.5f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("SlopeBias");
        ImGui.SliderFloat("##ShSlopeBias", ref shadow.SlopeBias, 0.001f, 0.01f, "%.4f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Strength");
        ImGui.SliderFloat("##ShStrength", ref shadow.Strength, 0f, 1f, "%.2f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("PcfRadius");
        ImGui.SliderFloat("##ShPcfRadius", ref shadow.PcfRadius, 0.5f, 4f, "%.2f");
        fieldStatus.NextFieldDrag();

        ImGui.EndGroup();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }

    private static void DrawLightState()
    {
        ref var dirLight = ref EditorDataStore.Slot<WorldParamsData>.State.SunLight;
        ref var ambientLight = ref EditorDataStore.Slot<WorldParamsData>.State.Ambient;

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

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }

    private static void DrawFogState()
    {
        var fieldStatus = new ImGuiFieldStatus();

        ref var fog = ref EditorDataStore.Slot<WorldParamsData>.State.Fog;
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

        ref var post = ref EditorDataStore.Slot<WorldParamsData>.State.PostEffect;
        ImGui.BeginGroup();
        ImGui.SeparatorText("Grade");
        ImGui.SliderFloat("##GrExposure", ref post.Grade.Exposure, 0.5f, 2f, "Exp: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##GrSaturation", ref post.Grade.Saturation, 0f, 1.5f, "Sat: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##GrContrast", ref post.Grade.Contrast, 0f, 1.5f, "Con: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##GrWarmth", ref post.Grade.Warmth, 0f, 1f, "War: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("White Balance");
        ImGui.SliderFloat("##WbTint", ref post.WhiteBalance.Tint, 0f, 1f, "Tint: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##WbStrength", ref post.WhiteBalance.Strength, -1f, 1f, "Str: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Bloom");
        ImGui.SliderFloat("##BlIntensity", ref post.Bloom.Intensity, 0f, 2f, "Int: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##BlThreshold", ref post.Bloom.Threshold, 0f, 2f, "Thr: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##BlRadius", ref post.Bloom.Radius, 0f, 10f, "Rad: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Image FX");
        ImGui.SliderFloat("##FxVignette", ref post.ImageFx.Vignette, 0f, 0.5f, "Vig: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##FxGrain", ref post.ImageFx.Grain, 0f, 0.5f, "Grn: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##FxSharpen", ref post.ImageFx.Sharpen, 0f, 0.5f, "Shr: %.2f");
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##FxRolloff", ref post.ImageFx.Rolloff, 0f, 0.5f, "Rol: %.2f");
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
                OnSelectionChange(WorldParamSelection.Post);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Shadow"))
            {
                OnSelectionChange(WorldParamSelection.Shadow);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.PopStyleVar(1);
    }
}