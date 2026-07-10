using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    

    private static IInspectField? MakeField(string fieldName, AttributeData attr, ITypeSymbol type)
    {
        string? typeName = null;
        string label = fieldName;
        if (!attr.ConstructorArguments.IsEmpty && attr.ConstructorArguments[0].Value is string displayName)
            label = displayName;


        int startAt = 0;
        bool hasAlpha = true;
        float min = 0, max = 0, speed = 0;
        string? format = null, placeholder = null;
        var style = InputStyle.Input;
        foreach (var (key, value) in attr.NamedArguments)
        {
            var v = value.Value;
            switch (key)
            {
                case "Converter" when v is INamedTypeSymbol l: typeName = l.Name; break;
                case "Min" when v is float l: min = l; break;
                case "Max" when v is float l: max = l; break;
                case "Speed" when v is float l: speed = l; break;
                case "Format" when v is string l: format = Symbols.FormatLiteral(l, quote: true); break;
                case "HasAlpha" when v is bool l: hasAlpha = l; break;
                case "StartAt" when v is int l: startAt = l; break;
                case "Placeholder" when v is string l: placeholder = l; break;
                case "Style" when v is byte l: style = (InputStyle)l; break;

            }
        }
        
        if (typeName is null)
        {
            var s = type.SpecialType;
            if (s == SpecialType.System_Enum) typeName = "Int1";
            else if (s == SpecialType.System_Single) typeName = "Float1";
            else if (s == SpecialType.System_Int32) typeName = "Int1";
            else if (DefaultInputMap.TryGetValue(type.Name, out var defaultValue)) typeName = defaultValue;
            else return null;
        }

        return attr.AttributeClass?.Name switch
        {
            InputColorAttrib => new ColorField { Name = fieldName, Label = label, HasAlpha = hasAlpha },
            InputComboAttrib => new ComboField { Name = fieldName, Label = label, Placeholder = placeholder, StartAt = startAt },
            InputNumberAttrib => new InputField
            {
                Name = fieldName,
                Label = label,
                ValueType = typeName,
                Format = format,
                Min = min,
                Max = max,
                Speed = speed,
                InputStyle = style,
                IsFloat = typeName.StartsWith("Float")
            },
            _ => null
        };
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