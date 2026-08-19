using System.Numerics;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.SpecialType;

namespace Generator.InspectorGen;

public sealed partial class InspectorGenerator
{
    private static (string? TypeName, bool IsFloat) GetDefaultValueType(string typeName) =>
        typeName switch
        {
            nameof(Vector2) => ("InputNumeric2", true),
            nameof(Vector3) => ("InputNumeric3", true),
            nameof(Vector4) => ("InputNumeric4", true),
            nameof(Quaternion) or "Color4" => ("InputNumeric4", true),
            "Size2D" => ("InputNumeric2", false),
            "Size3D" => ("InputNumeric3", false),
            "Int2" => ("InputNumeric2", false),
            "Int3" => ("InputNumeric3", false),
            "Int4" => ("InputNumeric4", false),
            _ => (null, false)
        };

    private static string? ComponentsToNumericName(int components) => components switch
    {
        1 => "InputNumeric1",
        2 => "InputNumeric2",
        3 => "InputNumeric3",
        4 => "InputNumeric4",
        _ => null
    };


    private static bool MemberFilter(ISymbol sym) =>
        sym is IPropertySymbol or IFieldSymbol && sym.DeclaredAccessibility == Accessibility.Public &&
        !sym.IsImplicitlyDeclared && sym.GetAttributes().Length > 0;

    private static void ExtractCommonFieldAttr(AttributeData attr, out string? label, out string? segment)
    {
        label = segment = null;
        foreach (var (key, value) in attr.NamedArguments)
        {
            var v = value.Value;
            switch (key)
            {
                case "Label" when v is string l: label = l; break;
                case "Segment" when v is string l: segment = l; break;
            }
        }
    }

    private static bool TryParseIncludeAttribute(ISymbol member, out string? nestedName)
    {
        nestedName = null;

        bool hasInclude = false;
        foreach (var attr in member.GetAttributes().Where(x => x.AttributeClass?.Name == IncludeAttrib))
        {
            if (attr.AttributeClass is null) continue;
            hasInclude = true;
            var ctor = attr.ConstructorArguments;
            if (!ctor.IsEmpty && ctor[0].Value is string accessSuffix) nestedName = accessSuffix;
        }

        return hasInclude;
    }


    private static NumberInput? MakeInputField(string fieldName, AttributeData attr, ITypeSymbol type)
    {
        var style = InputStyle.Input;
        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is byte b)
            style = (InputStyle)b;

        (string? TypeName, bool IsFloat, bool ImplicitCast) valueInfo = (null, false, false);

        float min = 0, max = 0, speed = 0;
        string? format = null;
        foreach (var (key, value) in attr.NamedArguments)
        {
            var v = value.Value;
            switch (key)
            {
                case "Min" when v is float l: min = l; break;
                case "Max" when v is float l: max = l; break;
                case "Speed" when v is float l: speed = l; break;
                case "Format" when v is string l: format = l; break;
                case "IsFloat" when v is bool l: valueInfo.IsFloat = l; break;
                case "Components" when v is int l:
                    valueInfo.ImplicitCast = false;
                    valueInfo.TypeName = ComponentsToNumericName(l);
                    break;
            }
        }

        if (valueInfo.TypeName is null)
        {
            var s = type.SpecialType;
            if (s is System_Single) valueInfo = ("InputNumeric1", true, true);
            else if (s is System_Int32 or System_Int16 or System_UInt16) valueInfo = ("InputNumeric1", false, true);
            else if (s is System_Enum) valueInfo = ("InputNumeric1", false, false);
            else
            {
                var (typeName, isFloat) = GetDefaultValueType(type.Name);
                valueInfo = (typeName, isFloat, false);
            }
        }

        if (valueInfo.TypeName is null) return null;

        var resultInfo = new NumericBindingInfo(valueInfo.TypeName, valueInfo.IsFloat, valueInfo.ImplicitCast);
        return new NumberInput(fieldName, resultInfo, style, speed, min, max, format);
    }


    private static ColorInput MakeColorField(string fieldName, AttributeData attr, ITypeSymbol type)
    {
        var hasAlpha = true;
        foreach (var (key, value) in attr.NamedArguments)
        {
            var v = value.Value;
            switch (key)
            {
                case "HasAlpha" when v is bool l: hasAlpha = l; break;
            }
        }

        return new ColorInput(fieldName, hasAlpha);
    }

    private static ComboInput? MakeComboField(string fieldName, AttributeData attr, ITypeSymbol type)
    {
        int startAt = 0;
        string? values = null, names = null, placeholder = null;

        foreach (var (key, value) in attr.NamedArguments)
        {
            switch (key)
            {
                case "StartAt" when value.Value is int l: startAt = l; break;
                case "Placeholder" when value.Value is string l: placeholder = l; break;
                case "Values" when value.Value is string l: values = $"[{l}]"; break;
                case "Names" when value.Value is string l:
                    var name = string.Join(", ", l.Split(", ").Select(static x => $"\"{x}\""));
                    names = $"[{name}]";
                    break;
                case "UseEnumExt" when value.Value is bool l:
                    if (!l) break;
                    var ns = type.ContainingNamespace.ToDisplayString();
                    values = $"{ns}.{type.Name}Ext.Values";
                    names = $"{ns}.{type.Name}Ext.Names";
                    break;
            }
        }

        if (names == null || values == null)
        {
            if (type.TypeKind == TypeKind.Enum)
            {
                var enumMembers = type.GetMembers().OfType<IFieldSymbol>().Where(f => f.HasConstantValue).ToArray();
                names = $"[{string.Join(", ", enumMembers.Select(static m => Symbols.FormatLiteral(m.Name, true)))}]";
                values = $"[{string.Join(", ", enumMembers.Select(static m => m.ConstantValue))}]";
            }
            else return null;
        }

        return new ComboInput(fieldName, values, names, placeholder, startAt);
    }
}