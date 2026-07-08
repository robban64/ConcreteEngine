namespace ConcreteEngine.Editor.Lib.Temp;

/*

   internal static class InspectorProvider
   {
       private static Camera Camera => Inspector<Camera>.Target!;
       public static void RegisterCamera()
       {
           Inspector<Camera>.Bind(EditorCamera.Instance.Camera);
           Inspector<Camera>.Register(
               new InputField<FloatInput>(
                   nameof(Camera.Translation), 
                   InputFieldKind.Input, 
                   FloatInput.Create(3, InputFieldKind.Input, format: "%.3f"),
                   static (ref v) => v.Value = (Float3)Camera.Translation,
                   static (ref v) => Camera.Translation = v.Reinterpret<Vector3>()
                   )
           );
           Inspector<Camera>.Register(
               new InputField<FloatInput>(
                   nameof(Camera.Orientation), 
                   InputFieldKind.Input, 
                   FloatInput.Create(2, InputFieldKind.Input, format: "%.3f"),
                   static (ref v) => v.Value = (Float2)Camera.Orientation,
                   static (ref v) => Camera.Orientation = new YawPitch(v.Value.X, v.Value.Y)
               )
           );
           Inspector<Camera>.Register(
               new InputField<FloatInput>(
                   nameof(Camera.NearFarPlane), 
                   InputFieldKind.Input, 
                   FloatInput.Create(2, InputFieldKind.Input),
                   static (ref v) => v.Value = (Float2)Camera.NearFarPlane,
                   static (ref v) => Camera.NearFarPlane = v.Reinterpret<Vector2>()
               )
           );
           Inspector<Camera>.Register(
               new InputField<FloatInput>(
                   nameof(Camera.Fov), 
                   InputFieldKind.Input, 
                   FloatInput.Create(1, InputFieldKind.Slider, format: "%.3f"),
                   static (ref v) => v.Value.X = Camera.Fov,
                   static (ref v) => Camera.Fov = v.Value.X
               )
           );
   
       }
   }
 

internal abstract class InputField
{
    private static int _currentId = 1;
    public readonly int DrawId;
    public readonly string Label;
    public InputFieldKind Widget { get; private set; }
    public InputTrigger Trigger = InputTrigger.OnChange;
    public FieldLabelPlacement LabelPlacement = FieldLabelPlacement.Top;


    public InputField(string label, InputFieldKind widget)
    {
        Label = label;
        Widget = widget;
        DrawId = _currentId++;
    }


    public abstract bool Draw();
    public abstract void Refresh();

    public abstract ref byte GetRawValue();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected NativeView<byte> ApplyLabelLayout(NativeSpanWriter sw)
    {
        switch (LabelPlacement)
        {
            case FieldLabelPlacement.Top:
                sw.Append(Label);
                AppDraw.Text(sw.End());
                ImGui.Separator();
                //ImGui.PushItemWidth(GuiTheme.FormItemWidth);
                break;
            case FieldLabelPlacement.Inline:
                sw.Append(Label);
                //ImGui.PushItemWidth(GuiTheme.FormItemInlineWidth);
                break;
        }

        return sw.AppendImGuiId(DrawId).End();
    }

    protected bool ShouldTrigger()
    {
        return Trigger switch
        {
            InputTrigger.OnChange => true,
            InputTrigger.AfterChange => ImGui.IsItemDeactivatedAfterEdit(),
            InputTrigger.AfterChangeDeactive => ImGui.IsItemDeactivatedAfterEdit() && !ImGui.IsItemActive(),
            _ => false
        };
    }
}

internal sealed unsafe class InputField<T> : InputField where T : unmanaged, IRawInput
{
    private T _input;
    private readonly Action<T> _getter;
    private readonly ActionRef<T> _setter;

    public InputField(string label, InputFieldKind widget, T input,ActionRef<T> getter, ActionRef<T> setter) : base(label, widget)
    {
        _input = input;
        _getter = getter;
        _setter = setter;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Draw()
    {
        ref var input = ref _input;
        _getter(ref input);

        var label = ApplyLabelLayout(TextBuffers.GetWriter());
        var changed = input.Draw(label);
        if (changed) _setter(ref input);
        return changed && ShouldTrigger();
    }

    public override void Refresh()
    {
        _getter?.Invoke(ref _input);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override ref byte GetRawValue() => ref _input.Reinterpret<byte>();
}

internal unsafe interface IRawInput
{
    bool Draw(byte* label);

    [UnscopedRef]
    ref T Reinterpret<T>() where T : unmanaged;
}

internal unsafe struct FloatInput : IRawInput
{
    public Float4 Value;

    public readonly int Components;
    public float Speed, Min, Max;
    public String8Utf8 Format;

    private readonly delegate*<int, byte*, float*, byte*, float, float, float, bool> _drawFunc;

    private FloatInput(int components, float speed, float min, float max, String8Utf8 format,
        delegate*<int, byte*, float*, byte*, float, float, float, bool> drawFunc)
    {
        Components = components;
        Speed = speed;
        Min = min;
        Max = max;
        Format = format;
        _drawFunc = drawFunc;
    }

    public static FloatInput Create(int components, InputFieldKind inputKind, float speed = 1f, float min = 0,
        float max = 0,
        string format = "%.2f")
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(components);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(components, 4);
        return new FloatInput(components, speed, min, max, format, InputFieldDrawer.BindFloat(inputKind));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Draw(byte* label)
    {
        var value = Value;
        var format = Format;
        var changed = _drawFunc(Components, label, (float*)&value, (byte*)&format, Speed, Min, Max);
        if (changed) Value = value;
        return changed;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Reinterpret<T>() where T : unmanaged => ref Unsafe.As<float, T>(ref Value.X);
}

internal unsafe struct IntInput : IRawInput
{
    public Int4 Value;

    public readonly int Components;
    public int Min, Max;
    public float Speed = 1f;

    private readonly delegate*<int, byte*, int*, float, int, int, bool> _drawFunc;

    private IntInput(int components, float speed, int min, int max,
        delegate*<int, byte*, int*, float, int, int, bool> drawFunc)
    {
        Components = components;
        Speed = speed;
        Min = min;
        Max = max;
        _drawFunc = drawFunc;
    }

    public static IntInput Create(int components, InputFieldKind inputKind, float speed = 1f, int min = 0, int max = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(components);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(components, 4);
        return new IntInput(components, speed, min, max, InputFieldDrawer.BindInt(inputKind));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Draw(byte* label)
    {
        var value = Value;
        var changed = _drawFunc(Components, label, (int*)&value, Speed, Min, Max);
        if (changed) Value = value;
        return changed;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Reinterpret<T>() where T : unmanaged => ref Unsafe.As<int, T>(ref Value.X);
}

internal unsafe struct ColorInput : IRawInput
{
    public Color4 Value;
    public bool HasAlpha;

    private ColorInput(bool hasAlpha)
    {
        HasAlpha = hasAlpha;
    }

    public static ColorInput Create(bool hasAlpha = true) => new ColorInput(hasAlpha);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Draw(byte* label)
    {
        var value = Value;
        var changed = HasAlpha
            ? ImGui.ColorEdit4(label, (float*)&value)
            : ImGui.ColorEdit3(label, (float*)&value);
        if (changed) Value = value;
        return changed;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Reinterpret<T>() where T : unmanaged => ref Unsafe.As<float, T>(ref Value.R);
}*/