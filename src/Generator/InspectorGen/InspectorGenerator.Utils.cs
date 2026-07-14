using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.SpecialType;

namespace Generator.InspectorGen;

public sealed partial class InspectorGenerator
{
    private static string? GetDefaultValueType(string typeName) => typeName switch
    {
        nameof(Vector2) => "Float2",
        nameof(Vector3) => "Float3",
        nameof(Vector4) => "Float4",
        nameof(Quaternion) => "Float4",
        "Color4" => "Float4",
        "Size2D" => "Int2",
        "Vector2I" => "Int2",
        _ => null
    };

    private static bool MemberFilter(ISymbol sym) =>
        sym is IPropertySymbol or IFieldSymbol && sym.DeclaredAccessibility == Accessibility.Public &&
        !sym.IsImplicitlyDeclared && sym.GetAttributes().Length > 0;

    private static void ExtractCommonFieldAttr(AttributeData attr, out string? displayName, out string? segment)
    {
        displayName = segment = null;
        foreach (var (key, value) in attr.NamedArguments)
        {
            var v = value.Value;
            switch (key)
            {
                case "DisplayName" when v is string l: displayName = l; break;
                case "Segment" when v is string l: segment = l; break;
            }
        }
    }

    private static bool TryParseIncludeAttribute(ISymbol member, out string? nestedName)
    {
        nestedName = null;

        var attr = member.GetAttributes().FirstOrDefault(static x => x.AttributeClass?.Name is IncludeAttrib);
        if (attr == null) return false;

        var ctor = attr.ConstructorArguments;
        if (!ctor.IsEmpty && ctor[0].Value is string accessSuffix) nestedName = accessSuffix;
        return true;
    }


    private static NumberInput? MakeInputField(string fieldName, AttributeData attr, ITypeSymbol type)
    {
        var style = InputStyle.Input;
        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is byte b)
            style = (InputStyle)b;

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
                case "Converter" when v is INamedTypeSymbol l: typeName = GetDefaultValueType(l.Name); break;
            }
        }
        
        if(typeName is null)
        {
            var s = type.SpecialType;
            if (s is System_Single) typeName = "Float1";
            else if (s is System_Enum or System_Int32 or System_Int16 or System_UInt16) typeName = "Int1";
            else if (GetDefaultValueType(type.Name) is {} defaultValueType ) typeName = defaultValueType;
            else return null;
        }

        return new NumberInput(fieldName, typeName, style, speed, min, max, format);
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
                    if(!l) break;
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