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
        
        { "Size2D", "Int2" },
        { "Vector2I", "Int2" }
    };
    
    
    private static IInspectField? MakeField(AttributeData attr, INamedTypeSymbol type)
    {
        if (attr.ConstructorArguments.IsEmpty || attr.ConstructorArguments[0].Value is not string typeName)
        {
            var s = type.SpecialType;
            if (s == SpecialType.System_Enum) typeName = "Int1";
            else if (s == SpecialType.System_Single) typeName = "Float1";
            else if (s == SpecialType.System_Int32) typeName = "Int1";
            else if (DefaultInputMap.TryGetValue(type.Name, out var defaultValue)) typeName = defaultValue!;
            else return null;
        }

        bool hasAlpha = true;

        int startAt = 0;
        string? placeholder = null;

        string? format = null;
        float min = 0, max = 0, speed = 0;
        foreach (var kv in attr.NamedArguments)
        {
            var v = kv.Value.Value;
            switch (kv.Key)
            {
                case "Min" when v is float l: min = l; break;
                case "Max" when v is float l: max = l; break;
                case "Speed" when v is float l: speed = l; break;
                case "Format" when v is string l: format = SymbolDisplay.FormatLiteral(l, quote: true); break;
                case "HasAlpha" when v is bool l: hasAlpha = l; break;
                case "StartAt" when v is int l: startAt = l; break;
                case "Placeholder" when v is string l: placeholder = l; break;
            }
        }

        return attr.AttributeClass?.Name switch
        {
            "ColorFieldAttribute" => new ColorField { HasAlpha = hasAlpha },
            "ComboFieldAttribute" => new ComboField { Placeholder = placeholder, StartAt = startAt },
            "InputFieldAttribute" => new InputField
            {
                ValueType = typeName,
                Format = format,
                Min = min,
                Max = max,
                Speed = speed
            },
            _ => null
        };
    }
}