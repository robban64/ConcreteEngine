using System.Text;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Scene;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Core.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Inspectors;

internal sealed unsafe class SceneInspectorPanel
{
    private const ImGuiTreeNodeFlags CollapseFlags = ImGuiTreeNodeFlags.DefaultOpen;
    private const string ValidNoneAlphaNumericChars = "_-";

    private readonly StateManager _state;
    private readonly SceneObjectInspector _inspector;
    private readonly ParticleInspector _particleInspector;

    private readonly NativeString _title;
    private readonly NativeString _nameInputStr;

    private SceneObjectId _previousId = SceneObjectId.Empty;


    public SceneInspectorPanel(StateManager state)
    {
        _state = state;
        _title = StringArena.AllocateString(24);
        _nameInputStr = StringArena.AllocateString(64);

        _inspector = new SceneObjectInspector();
        _particleInspector = new ParticleInspector();
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

    public void Draw()
    {
        if (SelectionManager.Instance.SelectedSceneObject is not { } sceneObject) return;

        if (_previousId != sceneObject.Id)
            OnNewInspector(sceneObject);

        //
        ImGui.PushStyleColor(ImGuiCol.Text, sceneObject.Kind.ToColor());
        ImGui.SeparatorText(_title);
        ImGui.PopStyleColor();

        //string
        ImGui.BeginGroup();
        if (AppDraw.Button(IconNames.Undo2))
            RestoreName(sceneObject);

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, _nameInputStr, 64, GuiTheme.InputNameFlags, InputCallback))
            HandleRename(sceneObject);

        ImGui.EndGroup();

        ImGui.Spacing();

        _inspector.Draw();
        if (sceneObject.TryGetInstance<ModelInstance>(out var modelInstance))
        {
            ImGui.Spacing();
            DrawModelInstance(modelInstance);
        }

        if (sceneObject.TryGetInstance<ParticleInstance>(out _))
            _particleInspector.Draw();
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
        _state.EnqueueEvent(new SceneObjectEvent(sceneObject.Id, Rename: name));
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