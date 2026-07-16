using System.Numerics;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.SpecialType;

namespace Generator.InspectorGen;

public sealed partial class InspectorGenerator
{
    private static (string? Type, bool IsFloat) GetDefaultValueType(string typeName) =>
        typeName switch
        {
            nameof(Vector2) => ("InputNumeric2", true),
            nameof(Vector3) => ("InputNumeric3", true),
            nameof(Vector4) or nameof(Quaternion) or "Color4" => ("InputNumeric4", true),
            "Size2D" => ("InputNumeric2", false),
            "Size3D" => ("InputNumeric3", false),
            "Int2" => ("InputNumeric2", false),
            "Int3" => ("InputNumeric3", false),
            "Int4" => ("InputNumeric4", false),
            _ => (null, false)
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

    private static bool TryParseIncludeAttribute(ISymbol member, out string? nestedName, out bool isInputGroup)
    {
        nestedName = null;
        isInputGroup = false;

        bool hasInclude = false;
        foreach (var attr in member.GetAttributes())
        {
            if (attr.AttributeClass is null) continue;
            var ctor = attr.ConstructorArguments;
            var name = attr.AttributeClass.Name;

            if (name is IncludeAttrib)
            {
                hasInclude = true;
                if (!ctor.IsEmpty && ctor[0].Value is string accessSuffix) nestedName = accessSuffix;
            }
            else if (name is InputGroupAttrib) isInputGroup = true;
        }

        return hasInclude;
    }


    private static NumberInput? MakeInputField(string fieldName, AttributeData attr, ITypeSymbol type)
    {
        var style = InputStyle.Input;
        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is byte b)
            style = (InputStyle)b;

        bool isFloat = false;
        float min = 0, max = 0, speed = 0;
        string? typeName = null, format = null;
        foreach (var (key, value) in attr.NamedArguments)
        {
            var v = value.Value;
            switch (key)
            {
                case "Min" when v is float l: min = l; break;
                case "Max" when v is float l: max = l; break;
                case "Speed" when v is float l: speed = l; break;
                case "Format" when v is string l: format = l; break;
                case "Converter" when v is INamedTypeSymbol l:
                    (typeName, isFloat) = GetDefaultValueType(l.Name);
                    break;
            }
        }

        if (typeName is null)
        {
            var s = type.SpecialType;
            if (s is System_Single) (typeName, isFloat) = ("InputNumeric1", true);
            else if (s is System_Enum or System_Int32 or System_Int16 or System_UInt16) typeName = "InputNumeric1";
            else if (GetDefaultValueType(type.Name) is { Type: not null } d) (typeName, isFloat) = d;
            else return null;
        }

        return new NumberInput(fieldName, typeName, isFloat, style, speed, min, max, format);
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
            else if (attr.ConstructorArguments.Length > 2)
            {
                values = attr.ConstructorArguments[1].Values.ToCollectionString();
                names = attr.ConstructorArguments[2].Values.ToCollectionString();
            }
            else return null;
        }

        return new ComboInput(fieldName, values, names, placeholder, startAt);
    }
}