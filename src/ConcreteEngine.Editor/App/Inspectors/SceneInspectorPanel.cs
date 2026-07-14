using System.Text;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Lib;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed unsafe class SceneInspectorPanel : EditorPanel
{
    private const ImGuiTreeNodeFlags CollapseFlags = ImGuiTreeNodeFlags.DefaultOpen;
    private const string ValidNoneAlphaNumericChars = "_-";

    private static SelectionManager Selection => SelectionManager.Instance;

    private SceneObjectId _previousId = SceneObjectId.Empty;

    private NativeString _title;
    private NativeString _nameInputStr;

    private readonly SceneObjectInspector _inspector;
    private readonly ParticleInspector _particleInspector;
    public SceneInspectorPanel(StateManager state) : base(InspectorId.SceneObject, state)
    {
        _inspector = new SceneObjectInspector();
        _particleInspector = new ParticleInspector();
    }

    public override void OnCreate()
    {
        _nameInputStr = StringArena.AllocateString(64);
        _title = StringArena.AllocateString(24);
    }
    
    public override void OnLeave()
    {
        _previousId = SceneObjectId.Empty;
    }

    
    private void OnNewInspector(SceneObject sceneObject)
    {
        RestoreName(sceneObject);
        _previousId = sceneObject.Id;
        _title.OverWriter.Append(sceneObject.Kind.ToUtf8()).Append(" - ["u8).Append(sceneObject.Id).Append(']').End();
    }

    private void RestoreName(SceneObject sceneObject)
    {
        _nameInputStr.Set(sceneObject.Name);
    }
    
    public override void OnDraw()
    {
        if (Selection.SelectedSceneObject is not { } sceneObject) return;

        if (_previousId != sceneObject.Id)
            OnNewInspector(sceneObject);

        //
        ImGui.PushStyleColor(ImGuiCol.Text, sceneObject.Kind.ToColor());
        ImGui.SeparatorText(_title);
        ImGui.PopStyleColor();

        //string
        ImGui.BeginGroup();
        if (ImGui.Button(StyleMap.GetIcon(Icons.Undo2)))
            RestoreName(sceneObject);

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, _nameInputStr, 64, GuiTheme.InputNameFlags, InputCallback))
            HandleRename(sceneObject);

        ImGui.EndGroup();

        ImGui.Spacing();
        
        if(ImGui.CollapsingHeader("Transform"u8, ImGuiTreeNodeFlags.DefaultOpen))
            SceneObjectInspector.Instance.DrawTransform();

        ImGui.Spacing();
        ImGui.Separator();

        if (sceneObject.TryGetInstance<ModelInstance>(out var modelInstance))
        {
            ImGui.Spacing();
            DrawModelInstance(modelInstance);
        }

        if (sceneObject.TryGetInstance<ParticleInstance>(out var particleInstance))
        {
            ImGui.Spacing();
            DrawParticles(particleInstance);
        }
    }

    private void DrawModelInstance(ModelInstance modelInstance)
    {
        var sw = ScratchBuffer.Writer();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.CollapsingHeader("Model Material"u8, CollapseFlags))
        {
            var materialCount = modelInstance.MaterialCount;
            for (var i = 0; i < materialCount; i++)
            {
                var mat = modelInstance.Blueprint.GetMaterial(i);
                sw.Append('[').Append(i).Append(']').PadRight(2);
                sw.Append(mat.Name).Append(' ', '(').Append(mat.BoundShader.Name).Append(')');
                AppDraw.Text(sw.End());
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

    private void DrawParticles(ParticleInstance particle)
    {
        var sw = ScratchBuffer.Writer();
        sw.Append("Particle Emitter: "u8);
        sw.Append(particle.Emitter.Name);
        if (ImGui.CollapsingHeader(sw.End(), CollapseFlags)) return;

        ParticleInspector.Instance.DrawEmitterParams();
        ParticleInspector.Instance.DrawParticleParams();

    }

    private void HandleRename(SceneObject sceneObject)
    {
        UtfText.SliceNullTerminate(_nameInputStr.Data.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;
        if (!UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        chars = chars.Trim();
        if (chars.IsEmpty || chars.Equals(sceneObject.Name, StringComparison.Ordinal)) return;

        var name = chars.ToString();
        State.EnqueueEvent(new SceneObjectEvent(sceneObject.Id, Rename: name));
    }

    private static int InputCallback(ImGuiInputTextCallbackData* data)
    {
        if (data->EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
        {
            var c = (char)data->EventChar;
            if (char.IsAsciiDigit(c) || char.IsAsciiLetterOrDigit(c) || ValidNoneAlphaNumericChars.Contains(c))
                return 0;

            return 1;
        }

        return 0;
    }
}