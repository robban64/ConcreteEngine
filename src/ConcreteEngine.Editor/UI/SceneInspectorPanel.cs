using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Bridge;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class SceneInspectorPanel(StateContext context) : EditorPanel(PanelId.SceneInspector, context)
{
    private const ImGuiTreeNodeFlags CollapseFlags = ImGuiTreeNodeFlags.DefaultOpen;
    private static readonly char[] ValidNoneAlphaNumericChars = ['_', '-'];
    
    private NativeViewPtr<byte> _inputStrPtr;
    private NativeViewPtr<byte> _titleStrPtr;

    private SceneObjectId _previousId = SceneObjectId.Empty;

    public override void OnCreate()
    {
        var block = AllocatePanelMemory(64+24);
        _inputStrPtr = block->AllocSlice(64);
        _titleStrPtr = block->AllocSlice(24);
    }


    public override void OnEnter()
    {
        if (Context.Selection.SelectedSceneObject is not { } inspector) return;
        inspector.SceneObjectFields.TranslationField.Refresh();
        inspector.SceneObjectFields.ScaleField.Refresh();
        inspector.SceneObjectFields.RotationField.Refresh();
    }

    public override void OnLeave()
    {
        PanelMemory->Data.Clear();
        _previousId = SceneObjectId.Empty;
    }

    public override void OnDraw(FrameContext ctx)
    {
        if (Context.Selection.SelectedSceneObject is not { } inspector) return;

        if (_previousId != inspector.Id)
            OnNewInspector(inspector);

        //
        ImGui.PushStyleColor(ImGuiCol.Text, StyleMap.GetSceneColor(inspector.Kind));
        ImGui.SeparatorText(_titleStrPtr);
        ImGui.PopStyleColor();

        //
        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(Icons.Undo2)))
            RestoreName(inspector.SceneObject);

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, _inputStrPtr, 64, GuiTheme.InputNameFlags, InputCallback))
            HandleRename(inspector);

        ImGui.EndGroup();

        DrawProperties(inspector, ctx);
    }

   

    private static void DrawProperties(InspectSceneObject inspector, FrameContext ctx)
    {
        ImGui.PushItemWidth(float.Min(GuiTheme.FormItemWidth, ImGui.GetContentRegionAvail().X));
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.CollapsingHeader("Transform"u8, CollapseFlags))
        {
            ImGui.Spacing();
            var fields = inspector.SceneObjectFields;
            fields.TranslationField.Draw();
            fields.ScaleField.Draw();
            fields.RotationField.Draw();
        }

        ImGui.Spacing();
        ImGui.Separator();
        if(inspector.InspectModel is {} modelInstance)
        {
            ImGui.Spacing();
            DrawModelInstance(inspector, modelInstance, ctx.Sw);
        }
        
        if (inspector.AnimationFields is { } animationFields)
        {
            ImGui.Spacing();
            DrawAnimation(inspector, animationFields, ctx.Sw);
        }

        if (inspector.ParticleFields is { } particleFields)
        {
            ImGui.Spacing();
            DrawParticles(particleFields, ctx.Sw);
        }

        ImGui.PopItemWidth();
    }

     private static void DrawModelInstance(InspectSceneObject inspector, InspectModelInstance modelInstance, UnsafeSpanWriter sw)
    {
        if(ImGui.CollapsingHeader("Local Spatial"u8))
        {
            ImGui.SeparatorText("Transform"u8);
            ImGui.Spacing();
            modelInstance.TranslationField.Draw();
            modelInstance.ScaleField.Draw();
            modelInstance.RotationField.Draw();
            ImGui.Spacing();
            ImGui.SeparatorText("Bounds"u8);
            ImGui.Spacing();
            modelInstance.LocalBoundsMinField.Draw();
            modelInstance.LocalBoundsMaxField.Draw();
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
                if (shader is null || shader.Id != mat.AssetShader)
                    shader = EngineObjectStore.AssetController.GetAsset<Shader>(mat.AssetShader);

                ImGui.TextUnformatted(sw.Append('[').Append(i).Append(']').PadRight(2).Append(mat.Name)
                    .Append(" ("u8).Append(shader.Name).Append(')').End());
            }
        }
    }

    private static void DrawAnimation(InspectSceneObject inspector, AnimationFields fields, UnsafeSpanWriter sw)
    {
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

    private static void DrawParticles(ParticleFields particle, UnsafeSpanWriter sw)
    {
        if (!ImGui.CollapsingHeader(sw.Append("Particle Emitter: "u8).Append(particle.EmitterName).End(),
                CollapseFlags))
        {
            return;
        }
        ImGui.Spacing();

        particle.ParticleCountField.Draw();

        ImGui.Spacing();
        ImGui.SeparatorText("Definition"u8);
        ImGui.Spacing();
        particle.StartColorField.Draw();
        particle.EndColorField.Draw();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        particle.GravityField.Draw();
        particle.DragField.Draw();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        particle.SpeedMinMaxField.Draw();
        particle.LifeMinMaxField.Draw();
        particle.SizeStartEndField.Draw();

        ImGui.Spacing();
        ImGui.SeparatorText("State"u8);
        ImGui.Spacing();
        particle.TranslationField.Draw();
        particle.StartAreaField.Draw();
        particle.DirectionField.Draw();
        particle.SpreadField.Draw();
    }

    private void OnNewInspector(InspectSceneObject inspector)
    {
        RestoreName(inspector.SceneObject);
        _previousId = inspector.Id;

        _titleStrPtr.Writer().Append(inspector.Kind.ToText()).Append(" - ["u8).Append(inspector.Id).Append(']').End();
    }
    
    private void RestoreName(SceneObject sceneObject)
    {
        _inputStrPtr.Clear();
        _inputStrPtr.Writer().Write(sceneObject.Name);
    }


    private void HandleRename(InspectSceneObject inspect)
    {
        UtfText.SliceNullTerminate(_inputStrPtr.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;
        if (!UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        chars = chars.Trim();
        if (chars.IsEmpty || chars.Equals(inspect.SceneObject.Name, StringComparison.Ordinal)) return;

        var name = chars.ToString();
        Context.EnqueueEvent(new SceneObjectEvent(EditorEvent.EventAction.Rename, inspect.Id, name));
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