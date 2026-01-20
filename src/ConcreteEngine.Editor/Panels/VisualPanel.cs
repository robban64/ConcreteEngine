using System.Numerics;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Panels.State;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class VisualPanel() : EditorPanel(PanelId.Visual)
{
    private int _editedField = -1;

    private VisualSelection _kind;

    public readonly SlotState<EditorVisualState> State = new();

    private readonly EnumTabBar<VisualSelection> _tabBar = new(0);

    private readonly SelectionCombo<int> _shadowSizeCombo =
        new(["1024px", "2048px", "4096px", "8192px"], [1024, 2048, 4096, 8192]);

    public override void Update()
    {
        EngineController.WorldController.FetchVisualParams(State.GetView());
    }

    private void OnUpdateShadowSize(SlotState<EditorVisualState> state, int size)
    {
        var existingSize = state.Data.Shadow.ShadowMapSize;
        if (size == existingSize) return;
        Context.EnqueueEvent(new GraphicsSettingsEvent() { ShadowSize = size });
    }

    private void OnSelectionChange(VisualSelection kind)
    {
        _kind = kind;
        _shadowSizeCombo.Sync(State.Data.Shadow.ShadowMapSize);
    }

    public override void Draw(ref FrameContext ctx)
    {
        _editedField = -1;

        if (_tabBar.Draw(out var value))
            OnSelectionChange(value);

        if (ImGui.BeginChild("##visual"u8, ImGuiChildFlags.AlwaysUseWindowPadding))
        {
            switch (_kind)
            {
                case VisualSelection.Light: DrawLightState(); break;
                case VisualSelection.Fog: DrawFogState(); break;
                case VisualSelection.Post: DrawPostEffects(); break;
                case VisualSelection.Shadow: DrawShadow(ref ctx); break;
                default: throw new ArgumentOutOfRangeException();
            }

            ImGui.EndChild();
        }

        if (_editedField >= 0)
        {
            Context.EnqueueEvent(new VisualDataEvent(State));
            _editedField = -1;
        }
    }


    private void DrawShadow(ref FrameContext ctx)
    {
        var fieldStatus = new FormFieldStatus();

        ref var shadow = ref State.Data.Shadow;
        int size = shadow.ShadowMapSize;

        ImGui.BeginGroup();
        ImGui.SeparatorText("Shadow Map Size"u8);
        ImGui.TextUnformatted(ctx.Sw.Write(size));

        if (_shadowSizeCombo.Draw("##shMapSize", out var newSize))
            OnUpdateShadowSize(State, newSize);

        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Shadow Setting"u8);

        ImGui.TextUnformatted("Distance"u8);
        ImGui.SliderFloat("##ShDist", ref shadow.Distance, 10f, 200f, "%.2f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("ZPad"u8);
        ImGui.SliderFloat("##ShZPad", ref shadow.ZPad, 1f, 200f, "%.2f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("ConstBias"u8);
        ImGui.SliderFloat("##ShConstBias", ref shadow.ConstBias, 0.0001f, 0.001f, "%.5f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("SlopeBias"u8);
        ImGui.SliderFloat("##ShSlopeBias", ref shadow.SlopeBias, 0.001f, 0.01f, "%.4f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Strength"u8);
        ImGui.SliderFloat("##ShStrength", ref shadow.Strength, 0f, 1f, "%.2f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("PcfRadius"u8);
        ImGui.SliderFloat("##ShPcfRadius", ref shadow.PcfRadius, 0.5f, 4f, "%.2f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.EndGroup();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }

    private void DrawLightState()
    {
        ref var dirLight = ref State.Data.SunLight;
        ref var ambientLight = ref State.Data.Ambient;

        var fieldStatus = new FormFieldStatus();

        ImGui.SeparatorText("Directional Light"u8);

        ImGui.TextUnformatted("Direction"u8);
        ImGui.DragFloat3("##LightDirection", ref dirLight.Direction, 0.01f, -1f, 1f, "%.2f");
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Diffuse"u8);
        ImGui.ColorEdit3("##LightDiffuse", ref dirLight.Diffuse);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Intensity"u8);
        ImGui.DragFloat("##LightIntensity", ref dirLight.Intensity, 0.01f, 0.0f, 10.0f, "%.3f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Specular"u8);
        ImGui.DragFloat("##LightSpecular", ref dirLight.Specular, 0.01f, 0.0f, 10.0f, "Spec: %.3f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.Dummy(new Vector2(0, 2));
        ImGui.SeparatorText("Ambient Light"u8);

        ImGui.TextUnformatted("Ambient"u8);
        ImGui.ColorEdit3("##LightAmbient", ref ambientLight.Ambient);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Ambient Ground"u8);
        ImGui.ColorEdit3("##LightAmbientGround", ref ambientLight.AmbientGround);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Exposure"u8);
        ImGui.DragFloat("##LightExposure"u8, ref ambientLight.Exposure, 0.01f, 0.0f, 2.0f, "Exp: %.3f"u8);
        fieldStatus.NextFieldDrag();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }

    private void DrawFogState()
    {
        var fieldStatus = new FormFieldStatus();

        ref var fog = ref State.Data.Fog;
        ImGui.SeparatorText("Fog Details"u8);
        ImGui.ColorEdit3("##FogColor", ref fog.Color);
        fieldStatus.NextFieldDrag();
        ImGui.TextUnformatted("Density"u8);
        ImGui.DragFloat("##FogDensity", ref fog.Density, 1f, 100, 1500, "Den: %.5f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Base Height"u8);
        ImGui.DragFloat("##FogBaseHeight", ref fog.BaseHeight, 1f, -1000f, 1000f, "Hgt: %.1f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Height Falloff"u8);
        ImGui.DragFloat("##FogHeightFalloff", ref fog.HeightFalloff, 1f, 0.001f, 10000.0f, "Fall: %.3f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Height Influence"u8);
        ImGui.DragFloat("##FogHeightInfluence", ref fog.HeightInfluence, 0.001f, 0f, 1f, "Inf: %.3f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.SeparatorText("Fog Optics"u8);
        ImGui.TextUnformatted("Scattering"u8);
        ImGui.DragFloat("##FogScattering", ref fog.Scattering, 0.001f, 0.0f, 1.0f, "Sct: %.3f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Max Distance"u8);
        ImGui.DragFloat("##FogMaxDistance", ref fog.MaxDistance, 1f, 1f, 10000f, "Dis: %.0f"u8);
        fieldStatus.NextFieldDrag();

        ImGui.TextUnformatted("Strength"u8);
        ImGui.DragFloat("##FogStrength", ref fog.Strength, 0.001f, 0f, 1f, "Str: %.3f"u8);
        fieldStatus.NextFieldDrag();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }

    private void DrawPostEffects()
    {
        var fieldStatus = new FormFieldStatus();

        ref var post = ref State.Data.PostEffect;
        ImGui.BeginGroup();
        ImGui.SeparatorText("Grade"u8);
        ImGui.SliderFloat("##GrExposure", ref post.Grade.Exposure, 0.5f, 2f, "Exp: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##GrSaturation", ref post.Grade.Saturation, 0f, 1.5f, "Sat: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##GrContrast", ref post.Grade.Contrast, 0f, 1.5f, "Con: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##GrWarmth", ref post.Grade.Warmth, 0f, 1f, "War: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("White Balance"u8);
        ImGui.SliderFloat("##WbTint", ref post.WhiteBalance.Tint, 0f, 1f, "Tint: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##WbStrength", ref post.WhiteBalance.Strength, -1f, 1f, "Str: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Bloom"u8);
        ImGui.SliderFloat("##BlIntensity", ref post.Bloom.Intensity, 0f, 2f, "Int: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##BlThreshold", ref post.Bloom.Threshold, 0f, 2f, "Thr: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##BlRadius", ref post.Bloom.Radius, 0f, 10f, "Rad: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        ImGui.Dummy(new Vector2(0, 2));

        ImGui.BeginGroup();
        ImGui.SeparatorText("Image FX"u8);
        ImGui.SliderFloat("##FxVignette"u8, ref post.ImageFx.Vignette, 0f, 0.5f, "Vig: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##FxGrain"u8, ref post.ImageFx.Grain, 0f, 0.5f, "Grn: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##FxSharpen"u8, ref post.ImageFx.Sharpen, 0f, 0.5f, "Shr: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.SliderFloat("##FxRolloff"u8, ref post.ImageFx.Rolloff, 0f, 0.5f, "Rol: %.2f"u8);
        fieldStatus.NextFieldDrag();
        ImGui.EndGroup();

        if (fieldStatus.HasEdited(out var field)) _editedField = field;
    }
}