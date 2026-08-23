using System.Diagnostics;

namespace Generator.InspectorGen;

internal abstract record InputField(string Name)
{
    public abstract string InputValueType { get; }
    public abstract string MakeSetterCast(InspectorMember member, string v);

    public abstract void AppendGetter(InspectorMember member, string accessPath, SourceBuilder sb);
}

internal readonly record struct NumericBindingInfo(string NumberType, bool IsFloat, bool ImplicitCast);

internal sealed record NumberInput(
    string Name,
    NumericBindingInfo NumericInfo,
    InputStyle Style,
    float Speed,
    float Min,
    float Max,
    string? Format) : InputField(Name)
{
    public int GetComponents() => (int)char.GetNumericValue(NumericInfo.NumberType[^1]);

    public override string InputValueType => NumericInfo.NumberType;

    public override string MakeSetterCast(InspectorMember member, string v)
    {
        if (NumericInfo.ImplicitCast) return $"({member.TypeName}){v}";
        return $"Unsafe.BitCast<{NumericInfo.NumberType}, {member.TypeName}>({v})";
    }

    public void AppendFieldName(SourceBuilder sb)
    {
        if (NumericInfo.IsFloat) sb.Builder.Append($"FloatInput<{NumericInfo.NumberType}> ");
        else sb.Builder.Append($"IntInput<{NumericInfo.NumberType}> ");
    }

    public void AppendFloatArgs(SourceBuilder sb)
    {
        sb.Append(Speed).Append(", ").Append(Min).Append(", ").Append(Max);
        if (!string.IsNullOrEmpty(Format)) sb.Append(", ").AppendLiteral(Format);
    }

    public void AppendIntArgs(SourceBuilder sb) =>
        sb.Append(Speed).Append(", ").Append((int)Min).Append(", ").Append((int)Max);


    public override void AppendGetter(InspectorMember member, string accessPath, SourceBuilder sb)
    {
        if (NumericInfo.ImplicitCast)
            sb.Append(accessPath);
        else
            sb.Builder.Append($"Unsafe.BitCast<{member.TypeName}, {NumericInfo.NumberType}>({accessPath})");
    }

    public string ToStyleString() =>
        Style switch
        {
            InputStyle.Input => "InputStyle.Input",
            InputStyle.Slider => "InputStyle.Slider",
            InputStyle.Drag => "InputStyle.Drag",
            _ => throw new UnreachableException()
        };
}

internal sealed record ColorInput(string Name, bool HasAlpha) : InputField(Name)
{
    public override string InputValueType => "Color4";

    public override void AppendGetter(InspectorMember member, string accessPath, SourceBuilder sb)
    {
        var typeName = member.TypeName;
        if (typeName.EndsWith("Vector3") || typeName.EndsWith("Vector4") || typeName.EndsWith("ColorRgba"))
            sb.Append("(Color4)");

        sb.Append(accessPath);
    }

    public override string MakeSetterCast(InspectorMember member, string v)
    {
        string castTo = v, typeName = member.TypeName;
        if (typeName.EndsWith("Vector3") || typeName.EndsWith("Vector4") || typeName.EndsWith("ColorRgba"))
            castTo = $"({member.TypeName}){v}";
        return castTo;
    }
}

internal sealed record ComboInput(string Name, string Values, string Names, string? Placeholder, int StartAt)
    : InputField(Name)
{
    public override string InputValueType => "int";

    public override string MakeSetterCast(InspectorMember member, string v)
    {
        return member.TypeName == "int" ? v : $"({member.TypeName}){v}";
    }


    public override void AppendGetter(InspectorMember member, string accessPath, SourceBuilder sb)
    {
        if (member.TypeName != "int")
            sb.Append("(int)");

        sb.Append(accessPath);
    }
}

internal sealed record CheckboxInput(string Name) : InputField(Name)
{
    public override string InputValueType => "bool";

    public override string MakeSetterCast(InspectorMember member, string v) => v;

    public override void AppendGetter(InspectorMember member, string accessPath, SourceBuilder sb)
    {
        sb.Append(accessPath);
    }
}