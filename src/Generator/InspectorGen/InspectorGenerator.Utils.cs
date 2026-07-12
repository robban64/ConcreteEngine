using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.SpecialType;

namespace Generator.InspectorGen;


public sealed partial class InspectorGenerator
{
    private static readonly Dictionary<string, string> DefaultInputMap = new()
    {
        { "Vector2", "Float2" },
        { "Vector3", "Float3" },
        { "Vector4", "Float4" },
        { "Color4", "Float4" },
        { "Quaternion", "Float4" },
        { "Size2D", "Int2" },
        { "Vector2I", "Int2" }
    };

    private static bool MemberFilter(ISymbol sym) =>
        sym is IPropertySymbol or IFieldSymbol && sym.DeclaredAccessibility == Accessibility.Public &&
        !sym.IsImplicitlyDeclared && sym.GetAttributes().Length > 0;

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
        var ctor = attr.ConstructorArguments;
        if (ctor.Length > 0 && ctor[0].Value is byte b) style = (InputStyle)b;

        float min = 0, max = 0, speed = 0;
        string? typeName = null, label = null, format = null;
        foreach (var (key, value) in attr.NamedArguments)
        {
            var v = value.Value;
            switch (key)
            {
                case "DisplayName" when v is string l: label = l; break;
                case "Converter" when v is INamedTypeSymbol l: typeName = l.Name; break;
                case "Min" when v is float l: min = l; break;
                case "Max" when v is float l: max = l; break;
                case "Speed" when v is float l: speed = l; break;
                case "Format" when v is string l: format = Symbols.FormatLiteral(l, quote: true); break;
            }
        }

        if (typeName is null)
        {
            var s = type.SpecialType;
            if (s is System_Single) typeName = "Float1";
            else if (s is System_Enum or System_Int32 or System_Int16 or System_UInt16) typeName = "Int1";
            else if (DefaultInputMap.TryGetValue(type.Name, out var defaultValue)) typeName = defaultValue;
            else return null;
        }

        return new NumberInput(fieldName, label ?? fieldName, typeName, style, speed, min, max, format);
    }

    private static ColorInput MakeColorField(string fieldName, AttributeData attr, ITypeSymbol type)
    {
        var hasAlpha = true;
        string label = fieldName;
        foreach (var (key, value) in attr.NamedArguments)
        {
            var v = value.Value;
            switch (key)
            {
                case "DisplayName" when v is string l: label = l; break;
                case "HasAlpha" when v is bool l: hasAlpha = l; break;
            }
        }

        return new ColorInput(fieldName, label, hasAlpha);
    }

    private static ComboInput? MakeComboField(string fieldName, AttributeData attr, ITypeSymbol type)
    {
        int startAt = 0;
        string? values, names, placeholder = null, label = null;
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
        else
        {
            return null;
        }

        foreach (var (key, value) in attr.NamedArguments)
        {
            switch (key)
            {
                case "DisplayName" when value.Value is string l: label = l; break;
                case "StartAt" when value.Value is int l: startAt = l; break;
                case "Placeholder" when value.Value is string l: placeholder = l; break;
            }
        }

        return new ComboInput(fieldName, label ?? fieldName, values, names, placeholder, startAt);
    }


    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol nested)
                foreach (var t in GetAllTypes(nested))
                    yield return t;
            else if (member is INamedTypeSymbol type)
                yield return type;
        }
    }
}