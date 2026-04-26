using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Inspector;
using ConcreteEngine.Editor.Inspector.Impl;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class SceneInspectorPanel(StateManager state) : EditorPanel(PanelId.SceneInspector, state)
{
    private const ImGuiTreeNodeFlags CollapseFlags = ImGuiTreeNodeFlags.DefaultOpen;
    private static readonly char[] ValidNoneAlphaNumericChars = ['_', '-'];

    private SceneObjectId _previousId = SceneObjectId.Empty;

    private readonly InspectSceneFields _inspectFields = InspectorFieldProvider.Instance.SceneFields;

    private readonly InspectModelInstanceFields _modelInstanceFields =
        InspectorFieldProvider.Instance.ModelInstanceFields;

    private readonly InspectParticleFields _particleInstanceFields =
        InspectorFieldProvider.Instance.ParticleInstanceFields;

    private RangeU16 _titleStrHandle;
    private RangeU16 _inputStrHandle;

    private NativeView<byte> TitleStr => DataPtr.Slice(_titleStrHandle);
    private NativeView<byte> InputStr => DataPtr.Slice(_inputStrHandle);


    public override void OnCreate()
    {
    }


    public override void OnEnter(ref MemoryBlockPtr memory)
    {
        _inputStrHandle = memory.AllocSlice(64).AsRange16();
        _titleStrHandle = memory.AllocSlice(24).AsRange16();

        if (State.Selection.SelectedSceneObject is not { } inspector) return;
        _inspectFields.Refresh();
    }

    public override void OnLeave()
    {
        _previousId = SceneObjectId.Empty;
    }

    public override void OnDraw()
    {
        if (State.Selection.SelectedSceneObject is not { } inspector) return;

        if (_previousId != inspector.Id)
            OnNewInspector(inspector);

        //
        ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetSceneColor(inspector.Kind));
        ImGui.SeparatorText(TitleStr);
        ImGui.PopStyleColor();

        //
        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(Icons.Undo2)))
            RestoreName(inspector.SceneObject);

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, InputStr, 64, GuiTheme.InputNameFlags, InputCallback))
            HandleRename(inspector);

        ImGui.EndGroup();

        ImGui.PushItemWidth(float.Min(GuiTheme.FormItemWidth, ImGui.GetContentRegionAvail().X));
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        _inspectFields.Draw();

        ImGui.Spacing();
        ImGui.Separator();

        if (inspector.InspectModel is { } modelInstance)
        {
            ImGui.Spacing();
            DrawModelInstance(inspector, modelInstance);
        }

        if (inspector.InspectAnimation is { } animationFields)
        {
            ImGui.Spacing();
            DrawAnimation(inspector, animationFields);
        }

        if (inspector.InspectParticle is { } particleFields)
        {
            ImGui.Spacing();
            DrawParticles(particleFields);
        }

        ImGui.PopItemWidth();
    }

    private void DrawModelInstance(InspectSceneObject inspector, InspectModelInstance modelInstance)
    {
        var sw = TextBuffers.GetWriter();

        if (ImGui.CollapsingHeader("Local Spatial"u8))
        {
            _modelInstanceFields.Draw();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.CollapsingHeader("Model Material"u8, CollapseFlags))
        {
            Shader? shader = null;
            var mats = modelInstance.GetMaterials();
            for (var i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (shader is null || shader.Id != mat.ShaderId)
                    shader = EngineObjectStore.AssetProvider.Get<Shader>(mat.ShaderId);

                AppDraw.Text(sw.Append('[').Append(i).Append(']').PadRight(2).Append(mat.Name)
                    .Append(" ("u8).Append(shader.Name).Append(')').End());
            }
        }
    }

    private static void DrawAnimation(InspectSceneObject inspector, InspectAnimationInstance fields)
    {
        var sw = TextBuffers.GetWriter();

        if (ImGui.CollapsingHeader("Animation"u8, CollapseFlags))
            return;

        var animation = fields.Instance.AssetAnimation;
        ImGui.TextUnformatted("Clips: "u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(sw.Write(animation.AnimationCount));
        ImGui.SameLine();
        ImGui.TextUnformatted("Bones: "u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(sw.Write(animation.BoneCount));
    }

    private void DrawParticles(InspectParticleInstance particle)
    {
        var sw = TextBuffers.GetWriter();

        if (ImGui.CollapsingHeader(sw.Append("Particle Emitter: "u8).Append(particle.EmitterName).End(), CollapseFlags))
        {
            return;
        }

        _particleInstanceFields.Draw();
    }

    private void OnNewInspector(InspectSceneObject inspector)
    {
        RestoreName(inspector.SceneObject);
        _previousId = inspector.Id;

        TitleStr.Clear();
        TitleStr.Writer().Append(inspector.Kind.ToText()).Append(" - ["u8).Append(inspector.Id).Append(']').End();
    }

    private void RestoreName(SceneObject sceneObject)
    {
        InputStr.Clear();
        InputStr.Writer().Write(sceneObject.Name);
    }

    private void HandleRename(InspectSceneObject inspect)
    {
        UtfText.SliceNullTerminate(InputStr.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;
        if (!UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        chars = chars.Trim();
        if (chars.IsEmpty || chars.Equals(inspect.SceneObject.Name, StringComparison.Ordinal)) return;

        var name = chars.ToString();
        State.EnqueueEvent(new SceneObjectEvent(EventAction.Rename, inspect.Id, name));
    }

    private static int InputCallback(ImGuiInputTextCallbackData* data)
    {
        if (data->EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
        {
            var c = (char)data->EventChar;
            if (char.IsAsciiDigit(c) || char.IsAsciiLetterOrDigit(c)) return 0;
            if (ValidNoneAlphaNumericChars.AsSpan().Contains(c)) return 0;
            return 1;
        }

        return 0;
    }
}