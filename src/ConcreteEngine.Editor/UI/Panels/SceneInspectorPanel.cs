using System.Text;
using ConcreteEngine.Core.Common.Text;
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

internal sealed unsafe class SceneInspectorPanel(StateManager state) : EditorPanel(InspectorId.SceneObject, state)
{
    private const ImGuiTreeNodeFlags CollapseFlags = ImGuiTreeNodeFlags.DefaultOpen;
    private static readonly char[] ValidNoneAlphaNumericChars = ['_', '-'];

    private static SelectionManager Selection => SelectionManager.Instance;

    private SceneObjectId _previousId = SceneObjectId.Empty;

    private readonly InspectSceneFields _inspectFields = InspectorFieldProvider.Instance.SceneFields;

    /*
    private readonly InspectModelInstanceFields _modelInstanceFields =
        InspectorFieldProvider.Instance.ModelInstanceFields;
*/
    private readonly InspectParticleFields _particleInstanceFields =
        InspectorFieldProvider.Instance.ParticleInstanceFields;


    private NativeString _title;
    private NativeString _nameInputStr;

    public override void OnCreate()
    {
        _nameInputStr = StringArena.AllocateString(64);
        _title = StringArena.AllocateString(24);
    }
    
    public override void OnAttach()
    {
        if (Selection.SelectedSceneObject is null) return;
        _inspectFields.Refresh();
    }
    
    public override void OnLeave()
    {
        _previousId = SceneObjectId.Empty;
    }

    public override void OnDraw()
    {
        if (Selection.SelectedSceneObject is not { } inspector) return;

        if (_previousId != inspector.Id)
            OnNewInspector(inspector);

        //
        ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetSceneColor(inspector.Kind));
        ImGui.SeparatorText(_title);
        ImGui.PopStyleColor();

        //
        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(Icons.Undo2)))
            RestoreName(inspector.SceneObject);

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, _nameInputStr, 64, GuiTheme.InputNameFlags, InputCallback))
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
            DrawModelInstance(inspector, modelInstance.Instance);
        }

        if (inspector.InspectParticle is { } particleFields)
        {
            ImGui.Spacing();
            DrawParticles(particleFields);
        }

        ImGui.PopItemWidth();
    }

    private void DrawModelInstance(InspectSceneObject inspector, ModelInstance modelInstance)
    {
        var sw = TextBuffers.GetWriter();
/*
        if (ImGui.CollapsingHeader("Local Spatial"u8))
        {
            _modelInstanceFields.Draw();
        }
*/
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.CollapsingHeader("Model Material"u8, CollapseFlags))
        {
            var materialCount = modelInstance.MaterialCount;
            for (var i = 0; i < materialCount; i++)
            {
                var mat = modelInstance.Blueprint.GetMaterial(i);
                var shaderName = mat.BoundShader?.Name ?? "No Shader";
                AppDraw.Text(sw.Append('[').Append(i).Append(']').PadRight(2).Append(mat.Name)
                    .Append(" ("u8).Append(shaderName).Append(')').End());
            }
        }

        if (modelInstance.Model.Rig is { } animation)
        {
            if (ImGui.CollapsingHeader("Animation"u8, CollapseFlags))
                return;

            ImGui.TextUnformatted("Clips: "u8);
            ImGui.SameLine();
            ImGui.TextUnformatted(sw.Write(animation.ClipCount));
            ImGui.SameLine();
            ImGui.TextUnformatted("Bones: "u8);
            ImGui.SameLine();
            ImGui.TextUnformatted(sw.Write(animation.BoneCount));
        }
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
        _title.NewWrite.Append(inspector.Kind.ToText()).Append(" - ["u8).Append((int)inspector.Id).Append(']').End();
    }

    private void RestoreName(SceneObject sceneObject)
    {
        _nameInputStr.Set(sceneObject.Name);
    }

    private void HandleRename(InspectSceneObject inspect)
    {
        UtfText.SliceNullTerminate(_nameInputStr.Data.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;
        if (!UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        chars = chars.Trim();
        if (chars.IsEmpty || chars.Equals(inspect.SceneObject.Name, StringComparison.Ordinal)) return;

        var name = chars.ToString();
        State.EnqueueEvent(new SceneObjectEvent(inspect.Id, Rename: name));
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