using System.Text;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.App.Inspectors;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.Scene;

[EditorInspector(typeof(SceneObject))]
internal sealed unsafe partial class SceneInspectorPanel : Inspector<SceneObject>
{
    private const ImGuiTreeNodeFlags CollapseFlags = ImGuiTreeNodeFlags.DefaultOpen;
    private const string ValidNoneAlphaNumericChars = "_-";

    private readonly StateManager _state;
    private Inspector? _instanceInspector;

    private readonly NativeString _title;
    private readonly NativeString _nameInputStr;

    public override InspectorId Id => InspectorId.SceneObject;
    public override uint Icon => IconNames.Box;

    public SceneInspectorPanel(StateManager state)
    {
        Sections = _fields.CreateSections();
        _ = new ParticleInspector();

        _state = state;
        _title = StringArena.AllocateString(24);
        _nameInputStr = StringArena.AllocateString(64);

    }

    protected override void OnAttachTarget(SceneObject? oldTarget, SceneObject newTarget)
    {
        RestoreName(newTarget);
        using var builder = new NativeStringBuilder(_title);
        builder.Writer.Append(newTarget.Kind.ToUtf8()).Append(" - ["u8).Append(newTarget.Id).Append(']');

        if (_instanceInspector != null)
        {
            _instanceInspector.DetachTarget();
            _instanceInspector = null;
        }
        /*
        if (newTarget.TryGetInstance<ModelInstance>(out var modelInstance))
        {
            _instanceInspector = ParticleInspector.Instance;
            ParticleInspector.Instance.AttachTarget(particleInstance.Emitter);
        }*/

        if (newTarget.TryGetInstance<ParticleInstance>(out var particleInstance))
        {
            _instanceInspector = ParticleInspector.Instance;
            ParticleInspector.Instance.AttachTarget(particleInstance.Emitter);
        }
    }

    private void RestoreName(SceneObject sceneObject) => _nameInputStr.Set(sceneObject.Name);


    public override void Draw()
    {
        //
        ImGui.PushStyleColor(ImGuiCol.Text, Target!.Kind.ToColor());
        ImGui.SeparatorText(_title);
        ImGui.PopStyleColor();

        //string
        ImGui.BeginGroup();
        if (AppDraw.Button(IconNames.Undo2))
            RestoreName(Target);

        ImGui.SameLine();
        if (ImGui.InputText("##name"u8, _nameInputStr, 64, GuiTheme.InputNameFlags, InputCallback))
            HandleRename();

        ImGui.EndGroup();

        ImGui.Spacing();

        foreach (var section in Sections) section.Draw();
        _instanceInspector?.Draw();

        /*
        if (sceneObject.TryGetInstance<ModelInstance>(out var modelInstance))
        {
            ImGui.Spacing();
            DrawModelInstance(modelInstance);
        }
        */
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
                sw.AppendAscii('[').Append(i).AppendAscii(']').PadRight(2);
                sw.Append(mat.Name).AppendAscii(' ', '(').Append(mat.BoundShader.Name).AppendAscii(')');
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

    private void HandleRename()
    {
        if (Target is null) Throwers.NullReference(nameof(Target));

        UtfText.SliceNullTerminate(_nameInputStr.Data.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;
        if (!UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        chars = chars.Trim();
        if (chars.IsEmpty || chars.Equals(Target.Name, StringComparison.Ordinal)) return;

        var name = chars.ToString();
        _state.EnqueueEvent(new SceneObjectEvent(Target.Id, Rename: name));
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